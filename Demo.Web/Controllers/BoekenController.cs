using Demo.Web.Data;
using Demo.Web.DTOs;
using Demo.Web.Models;
using Demo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Demo.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BoekenController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IRabbitMqService _rabbitMqService;
    private readonly ILogger<BoekenController> _logger;

    public BoekenController(
        AppDbContext context,
        IRabbitMqService rabbitMqService,
        ILogger<BoekenController> logger)
    {
        _context = context;
        _rabbitMqService = rabbitMqService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BoekDto>>> GetBoeken()
    {
        var boeken = await _context.Boeken
            .Select(b => new BoekDto
            {
                Id = b.Id,
                Titel = b.Titel,
                Auteur = b.Auteur,
                Prijs = b.Prijs,
                VoorraadAantal = b.VoorraadAantal,
                ISBN = b.ISBN
            })
            .ToListAsync();

        return Ok(boeken);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BoekDto>> GetBoek(int id)
    {
        var boek = await _context.Boeken.FindAsync(id);

        if (boek == null)
        {
            return NotFound(new { error = "Boek niet gevonden" });
        }

        var boekDto = new BoekDto
        {
            Id = boek.Id,
            Titel = boek.Titel,
            Auteur = boek.Auteur,
            Prijs = boek.Prijs,
            VoorraadAantal = boek.VoorraadAantal,
            ISBN = boek.ISBN
        };

        return Ok(boekDto);
    }

    [HttpGet("{id}/voorraad")]
    public async Task<ActionResult<object>> GetVoorraad(int id)
    {
        var boek = await _context.Boeken.FindAsync(id);

        if (boek == null)
        {
            return NotFound(new { error = "Boek niet gevonden" });
        }

        return Ok(new
        {
            boekId = boek.Id,
            titel = boek.Titel,
            voorraadAantal = boek.VoorraadAantal,
            inStock = boek.VoorraadAantal > 0
        });
    }

    [HttpPost]
    public async Task<ActionResult<BoekDto>> CreateBoek(BoekDto boekDto)
    {
        var boek = new Boek
        {
            Titel = boekDto.Titel,
            Auteur = boekDto.Auteur,
            Prijs = boekDto.Prijs,
            VoorraadAantal = boekDto.VoorraadAantal,
            ISBN = boekDto.ISBN
        };

        _context.Boeken.Add(boek);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Boek aangemaakt: {boek.Titel} (ID: {boek.Id})");

        boekDto.Id = boek.Id;
        return CreatedAtAction(nameof(GetBoek), new { id = boek.Id }, boekDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBoek(int id, BoekDto boekDto)
    {
        var boek = await _context.Boeken.FindAsync(id);

        if (boek == null)
        {
            return NotFound(new { error = "Boek niet gevonden" });
        }

        boek.Titel = boekDto.Titel;
        boek.Auteur = boekDto.Auteur;
        boek.Prijs = boekDto.Prijs;
        boek.VoorraadAantal = boekDto.VoorraadAantal;
        boek.ISBN = boekDto.ISBN;

        await _context.SaveChangesAsync();

        _logger.LogInformation($"Boek bijgewerkt: {boek.Titel} (ID: {boek.Id})");

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBoek(int id)
    {
        var boek = await _context.Boeken.FindAsync(id);

        if (boek == null)
        {
            return NotFound(new { error = "Boek niet gevonden" });
        }

        // Check if boek is used in orders
        var usedInOrders = await _context.OrderRegels.AnyAsync(or => or.BoekId == id);
        if (usedInOrders)
        {
            return BadRequest(new { error = "Kan boek niet verwijderen. Het boek is gebruikt in bestellingen." });
        }

        // Create delete message BEFORE deleting from database
        var deleteMessage = new BoekDeletedMessage
        {
            BoekId = boek.Id,
            Titel = boek.Titel,
            ISBN = boek.ISBN,
            LaatsteVoorraad = boek.VoorraadAantal,
            DeletedAt = DateTime.UtcNow,
            Reason = "User requested deletion via API"
        };

        // Also create entity change message
        var entityChange = new EntityChangeMessage
        {
            EntityType = EntityType.Boek,
            Action = ActionType.Deleted,
            EntityId = boek.Id,
            EntityName = boek.Titel,
            Timestamp = DateTime.UtcNow,
            Data = new Dictionary<string, object>
            {
                { "isbn", boek.ISBN },
                { "auteur", boek.Auteur },
                { "prijs", boek.Prijs },
                { "voorraad", boek.VoorraadAantal }
            }
        };

        // Delete from database
        _context.Boeken.Remove(boek);
        await _context.SaveChangesAsync();

        // Publish to RabbitMQ (async, don't wait)
        _ = Task.Run(async () =>
        {
            try
            {
                await _rabbitMqService.PublishEntityChangeAsync("boek_deleted", deleteMessage);
                await _rabbitMqService.PublishEntityChangeAsync("entity_changes", entityChange);
                _logger.LogInformation($"Boek delete event gepubliceerd naar RabbitMQ: {boek.Titel} (ID: {boek.Id})");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Fout bij publiceren delete event naar RabbitMQ: {ex.Message}");
            }
        });

        _logger.LogInformation($"Boek verwijderd: {boek.Titel} (ID: {boek.Id})");

        return NoContent();
    }
}
