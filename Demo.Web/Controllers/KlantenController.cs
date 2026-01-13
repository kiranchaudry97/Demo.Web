using Demo.Web.Data;
using Demo.Web.DTOs;
using Demo.Web.Models;
using Demo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Demo.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KlantenController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IRabbitMqService _rabbitMqService;
    private readonly ILogger<KlantenController> _logger;

    public KlantenController(
        AppDbContext context, 
        IRabbitMqService rabbitMqService,
        ILogger<KlantenController> logger)
    {
        _context = context;
        _rabbitMqService = rabbitMqService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<KlantDto>>> GetKlanten()
    {
        var klanten = await _context.Klanten
            .Select(k => new KlantDto
            {
                Id = k.Id,
                Naam = k.Naam,
                Email = k.Email,
                Telefoon = k.Telefoon,
                Adres = k.Adres
            })
            .ToListAsync();

        return Ok(klanten);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<KlantDto>> GetKlant(int id)
    {
        var klant = await _context.Klanten.FindAsync(id);

        if (klant == null)
        {
            return NotFound(new { error = "Klant niet gevonden" });
        }

        var klantDto = new KlantDto
        {
            Id = klant.Id,
            Naam = klant.Naam,
            Email = klant.Email,
            Telefoon = klant.Telefoon,
            Adres = klant.Adres
        };

        return Ok(klantDto);
    }

    [HttpPost]
    public async Task<ActionResult<KlantDto>> CreateKlant(KlantDto klantDto)
    {
        var klant = new Klant
        {
            Naam = klantDto.Naam,
            Email = klantDto.Email,
            Telefoon = klantDto.Telefoon,
            Adres = klantDto.Adres
        };

        _context.Klanten.Add(klant);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Klant aangemaakt: {klant.Naam} (ID: {klant.Id})");

        klantDto.Id = klant.Id;
        return CreatedAtAction(nameof(GetKlant), new { id = klant.Id }, klantDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateKlant(int id, KlantDto klantDto)
    {
        var klant = await _context.Klanten.FindAsync(id);

        if (klant == null)
        {
            return NotFound(new { error = "Klant niet gevonden" });
        }

        klant.Naam = klantDto.Naam;
        klant.Email = klantDto.Email;
        klant.Telefoon = klantDto.Telefoon;
        klant.Adres = klantDto.Adres;

        await _context.SaveChangesAsync();

        _logger.LogInformation($"Klant bijgewerkt: {klant.Naam} (ID: {klant.Id})");

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteKlant(int id)
    {
        var klant = await _context.Klanten.FindAsync(id);

        if (klant == null)
        {
            return NotFound(new { error = "Klant niet gevonden" });
        }

        // Check if klant has orders
        var hasOrders = await _context.Orders.AnyAsync(o => o.KlantId == id);
        if (hasOrders)
        {
            return BadRequest(new { error = "Kan klant niet verwijderen. Er zijn nog orders gekoppeld aan deze klant." });
        }

        // Create delete message BEFORE deleting from database
        var deleteMessage = new KlantDeletedMessage
        {
            KlantId = klant.Id,
            KlantNaam = klant.Naam,
            Email = klant.Email,
            DeletedAt = DateTime.UtcNow,
            Reason = "User requested deletion via API"
        };

        // Also create entity change message
        var entityChange = new EntityChangeMessage
        {
            EntityType = EntityType.Klant,
            Action = ActionType.Deleted,
            EntityId = klant.Id,
            EntityName = klant.Naam,
            Timestamp = DateTime.UtcNow,
            Data = new Dictionary<string, object>
            {
                { "email", klant.Email },
                { "telefoon", klant.Telefoon }
            }
        };

        // Delete from database
        _context.Klanten.Remove(klant);
        await _context.SaveChangesAsync();

        // Publish to RabbitMQ (async, don't wait)
        _ = Task.Run(async () =>
        {
            try
            {
                await _rabbitMqService.PublishEntityChangeAsync("klant_deleted", deleteMessage);
                await _rabbitMqService.PublishEntityChangeAsync("entity_changes", entityChange);
                _logger.LogInformation($"Klant delete event gepubliceerd naar RabbitMQ: {klant.Naam} (ID: {klant.Id})");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Fout bij publiceren delete event naar RabbitMQ: {ex.Message}");
            }
        });

        _logger.LogInformation($"Klant verwijderd: {klant.Naam} (ID: {klant.Id})");

        return NoContent();
    }
}
