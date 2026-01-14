using Demo.Web.Data;
using Demo.Web.DTOs;
using Demo.Web.Models;
using Demo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Demo.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IRabbitMqService _rabbitMqService;
    private readonly IMessageValidationService _validationService;
    private readonly ISapService _sapService;
    private readonly ISalesforceService _salesforceService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        AppDbContext context,
        IRabbitMqService rabbitMqService,
        IMessageValidationService validationService,
        ISapService sapService,
        ISalesforceService salesforceService,
        ILogger<OrdersController> logger)
    {
        _context = context;
        _rabbitMqService = rabbitMqService;
        _validationService = validationService;
        _sapService = sapService;
        _salesforceService = salesforceService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.Klant)
            .Include(o => o.OrderRegels)
            .ThenInclude(or => or.Boek)
            .Select(o => new
            {
                o.Id,
                o.OrderNummer,
                o.OrderDatum,
                o.Status,
                o.TotaalBedrag,
                o.SalesforceId,
                o.SapStatus,
                Klant = new { o.Klant!.Id, o.Klant.Naam },
                AantalItems = o.OrderRegels.Count
            })
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetOrder(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Klant)
            .Include(o => o.OrderRegels)
            .ThenInclude(or => or.Boek)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound(new { error = "Order niet gevonden" });
        }

        return Ok(new
        {
            order.Id,
            order.OrderNummer,
            order.OrderDatum,
            order.Status,
            order.TotaalBedrag,
            order.SalesforceId,
            order.SapStatus,
            Klant = new
            {
                order.Klant!.Id,
                order.Klant.Naam,
                order.Klant.Email,
                order.Klant.Telefoon
            },
            Items = order.OrderRegels.Select(or => new
            {
                or.Id,
                or.BoekId,
                Titel = or.Boek!.Titel,
                or.Aantal,
                or.Prijs,
                Subtotaal = or.Aantal * or.Prijs
            })
        });
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponseDto>> CreateOrder(OrderCreateDto orderDto)
    {
        if (orderDto.Items == null || !orderDto.Items.Any())
        {
            return BadRequest(new { error = "Order moet minimaal 1 item bevatten" });
        }

        var klant = await _context.Klanten.FindAsync(orderDto.KlantId);
        if (klant == null)
        {
            return NotFound(new { error = "Klant niet gevonden" });
        }

        var boekIds = orderDto.Items.Select(i => i.BoekId).ToList();
        var boeken = await _context.Boeken
            .Where(b => boekIds.Contains(b.Id))
            .ToListAsync();

        foreach (var item in orderDto.Items)
        {
            var boek = boeken.FirstOrDefault(b => b.Id == item.BoekId);
            if (boek == null)
            {
                return NotFound(new { error = $"Boek met ID {item.BoekId} niet gevonden" });
            }

            if (boek.VoorraadAantal < item.Aantal)
            {
                return BadRequest(new
                {
                    error = $"Onvoldoende voorraad voor '{boek.Titel}'",
                    beschikbaar = boek.VoorraadAantal,
                    gevraagd = item.Aantal
                });
            }
        }

        var order = new Order
        {
            KlantId = orderDto.KlantId,
            OrderDatum = DateTime.UtcNow,
            OrderNummer = $"ORD{DateTime.UtcNow:yyyyMMddHHmmss}",
            Status = "In behandeling"
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        foreach (var item in orderDto.Items)
        {
            var boek = boeken.First(b => b.Id == item.BoekId);
            
            var orderRegel = new OrderRegel
            {
                OrderId = order.Id,
                BoekId = item.BoekId,
                Aantal = item.Aantal,
                Prijs = boek.Prijs
            };

            _context.OrderRegels.Add(orderRegel);
            
            boek.VoorraadAantal -= item.Aantal;
        }

        order.TotaalBedrag = orderDto.Items.Sum(item =>
        {
            var boek = boeken.First(b => b.Id == item.BoekId);
            return item.Aantal * boek.Prijs;
        });

        await _context.SaveChangesAsync();

        await _context.Entry(order).Reference(o => o.Klant).LoadAsync();
        await _context.Entry(order).Collection(o => o.OrderRegels).LoadAsync();
        foreach (var regel in order.OrderRegels)
        {
            await _context.Entry(regel).Reference(r => r.Boek).LoadAsync();
        }

        _logger.LogInformation($"Order aangemaakt: {order.OrderNummer}");

        var salesforceTask = Task.Run(async () =>
        {
            try
            {
                var orderMessage = new OrderMessage
                {
                    OrderNummer = order.OrderNummer,
                    KlantNaam = klant.Naam,
                    KlantEmail = klant.Email,
                    OrderDatum = order.OrderDatum,
                    TotaalBedrag = order.TotaalBedrag,
                    Items = order.OrderRegels.Select(r => new OrderItemMessage
                    {
                        BoekTitel = r.Boek!.Titel,
                        Aantal = r.Aantal,
                        Prijs = r.Prijs
                    }).ToList()
                };

                // ? Validate message before publishing
                var (isValid, errors) = await _validationService.ValidateAndLogAsync(orderMessage, "OrderMessage");
                
                if (isValid)
                {
                    // ?? Publish to RabbitMQ (salesforce_orders queue)
                    await _rabbitMqService.PublishOrderToSalesforceAsync(orderMessage);
                    _logger.LogInformation($"?? Order gepubliceerd naar RabbitMQ: {order.OrderNummer}");
                }
                else
                {
                    _logger.LogWarning($"?? Order message validation failed: {string.Join(", ", errors)}");
                }
                
                // Direct Salesforce call (backup voor als consumer niet draait)
                var salesforceId = await _salesforceService.CreateOrderAsync(orderMessage);
                
                order.SalesforceId = salesforceId;
                await _context.SaveChangesAsync();

                return salesforceId;
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Salesforce integratie mislukt: {ex.Message}");
                return "ERROR";
            }
        });

        var sapTask = Task.Run(async () =>
        {
            try
            {
                var sapResponse = await _sapService.SendOrderToSapAsync(order);
                
                order.SapStatus = sapResponse.Status;
                await _context.SaveChangesAsync();

                return sapResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError($"SAP integratie mislukt: {ex.Message}");
                return new SapResponse { Success = false, Status = "51", Message = ex.Message };
            }
        });

        await Task.WhenAll(salesforceTask, sapTask);

        var salesforceId = await salesforceTask;
        var sapResponse = await sapTask;

        var response = new OrderResponseDto
        {
            OrderId = order.Id,
            OrderNummer = order.OrderNummer,
            Status = order.Status,
            TotaalBedrag = order.TotaalBedrag,
            OrderDatum = order.OrderDatum,
            SalesforceId = salesforceId,
            SapStatus = $"Status {sapResponse.Status}: {sapResponse.Message}",
            Bericht = "Order succesvol aangemaakt en verstuurd naar Salesforce (via RabbitMQ) en SAP"
        };

        _logger.LogInformation($"Order {order.OrderNummer} - Salesforce: {salesforceId}, SAP: {sapResponse.Status}");

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, response);
    }
}
