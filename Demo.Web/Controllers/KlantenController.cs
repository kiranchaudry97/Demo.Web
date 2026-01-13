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
    private readonly IRabbitMqAdvancedService _rabbitMqAdvancedService;
    private readonly IEncryptionService _encryptionService;
    private readonly IMessageValidationService _validationService;
    private readonly ILogger<KlantenController> _logger;

    public KlantenController(
        AppDbContext context, 
        IRabbitMqService rabbitMqService,
        IRabbitMqAdvancedService rabbitMqAdvancedService,
        IEncryptionService encryptionService,
        IMessageValidationService validationService,
        ILogger<KlantenController> logger)
    {
        _context = context;
        _rabbitMqService = rabbitMqService;
        _rabbitMqAdvancedService = rabbitMqAdvancedService;
        _encryptionService = encryptionService;
        _validationService = validationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<KlantDto>>> GetKlanten()
    {
        var klanten = await _context.Klanten.ToListAsync();
        
        // ?? Decrypt PII data before returning
        var klantDtos = klanten.Select(k => new KlantDto
        {
            Id = k.Id,
            Naam = k.Naam,
            Email = _encryptionService.Decrypt(k.Email),
            Telefoon = _encryptionService.Decrypt(k.Telefoon),
            Adres = _encryptionService.Decrypt(k.Adres)
        }).ToList();

        return Ok(klantDtos);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<KlantDto>>> SearchKlanten([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { error = "Zoekterm is verplicht" });
        }

        var lowerQuery = query.ToLower();

        // ?? Note: Searching encrypted data requires full table scan and decryption
        // For production: Use separate searchable index or hash-based search
        var allKlanten = await _context.Klanten.ToListAsync();
        
        var klanten = allKlanten
            .Where(k => 
            {
                var email = _encryptionService.Decrypt(k.Email);
                var telefoon = _encryptionService.Decrypt(k.Telefoon);
                return k.Naam.ToLower().Contains(lowerQuery) ||
                       email.ToLower().Contains(lowerQuery) ||
                       telefoon.Contains(query);
            })
            .Select(k => new KlantDto
            {
                Id = k.Id,
                Naam = k.Naam,
                Email = _encryptionService.Decrypt(k.Email),
                Telefoon = _encryptionService.Decrypt(k.Telefoon),
                Adres = _encryptionService.Decrypt(k.Adres)
            })
            .ToList();

        _logger.LogInformation($"Zoekterm '{query}' leverde {klanten.Count} klanten op");

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

        // ?? Decrypt PII data
        var klantDto = new KlantDto
        {
            Id = klant.Id,
            Naam = klant.Naam,
            Email = _encryptionService.Decrypt(klant.Email),
            Telefoon = _encryptionService.Decrypt(klant.Telefoon),
            Adres = _encryptionService.Decrypt(klant.Adres)
        };

        return Ok(klantDto);
    }

    [HttpPost]
    public async Task<ActionResult<KlantDto>> CreateKlant(KlantDto klantDto)
    {
        // ?? Encrypt PII data before storing
        var klant = new Klant
        {
            Naam = klantDto.Naam,
            Email = _encryptionService.Encrypt(klantDto.Email),
            Telefoon = _encryptionService.Encrypt(klantDto.Telefoon),
            Adres = _encryptionService.Encrypt(klantDto.Adres)
        };

        _context.Klanten.Add(klant);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"?? Klant aangemaakt met encrypted PII: {klant.Naam} (ID: {klant.Id})");

        // ?? Publish entity created event to RabbitMQ (Advanced Pattern)
        var entityChange = new EntityChangeMessage
        {
            EntityType = EntityType.Klant,
            Action = ActionType.Created,
            EntityId = klant.Id,
            EntityName = klant.Naam,
            Timestamp = DateTime.UtcNow,
            Data = new Dictionary<string, object>
            {
                { "email", klantDto.Email }, // Use decrypted for messaging
                { "telefoon", klantDto.Telefoon }
            }
        };

        // Validate and publish
        var (isValid, errors) = await _validationService.ValidateAndLogAsync(entityChange, "EntityChangeMessage");
        if (isValid)
        {
            await _rabbitMqAdvancedService.PublishEntityEventAsync("klant", "created", entityChange);
        }

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

        // ?? Encrypt PII data before updating
        klant.Naam = klantDto.Naam;
        klant.Email = _encryptionService.Encrypt(klantDto.Email);
        klant.Telefoon = _encryptionService.Encrypt(klantDto.Telefoon);
        klant.Adres = _encryptionService.Encrypt(klantDto.Adres);

        await _context.SaveChangesAsync();

        _logger.LogInformation($"?? Klant bijgewerkt met encrypted PII: {klant.Naam} (ID: {klant.Id})");

        // ?? Publish entity updated event
        var entityChange = new EntityChangeMessage
        {
            EntityType = EntityType.Klant,
            Action = ActionType.Updated,
            EntityId = klant.Id,
            EntityName = klant.Naam,
            Timestamp = DateTime.UtcNow,
            Data = new Dictionary<string, object>
            {
                { "email", klantDto.Email },
                { "telefoon", klantDto.Telefoon }
            }
        };

        var (isValid, errors) = await _validationService.ValidateAndLogAsync(entityChange, "EntityChangeMessage");
        if (isValid)
        {
            await _rabbitMqAdvancedService.PublishEntityEventAsync("klant", "updated", entityChange);
        }

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

        // ?? Decrypt data before creating delete message
        var decryptedEmail = _encryptionService.Decrypt(klant.Email);
        var decryptedTelefoon = _encryptionService.Decrypt(klant.Telefoon);

        // Create delete message BEFORE deleting from database
        var deleteMessage = new KlantDeletedMessage
        {
            KlantId = klant.Id,
            KlantNaam = klant.Naam,
            Email = decryptedEmail, // Use decrypted for messaging
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
                { "email", decryptedEmail },
                { "telefoon", decryptedTelefoon }
            }
        };

        // Validate messages before publishing
        var (isValidDelete, deleteErrors) = await _validationService.ValidateAndLogAsync(deleteMessage, "KlantDeletedMessage");
        var (isValidChange, changeErrors) = await _validationService.ValidateAndLogAsync(entityChange, "EntityChangeMessage");

        // Delete from database
        _context.Klanten.Remove(klant);
        await _context.SaveChangesAsync();

        // ?? Publish to RabbitMQ using Advanced Patterns
        _ = Task.Run(async () =>
        {
            try
            {
                if (isValidDelete)
                {
                    // Use topic exchange with routing key
                    await _rabbitMqAdvancedService.PublishEntityEventAsync("klant", "deleted", deleteMessage);
                }
                
                if (isValidChange)
                {
                    await _rabbitMqAdvancedService.PublishEntityEventAsync("klant", "deleted", entityChange);
                }
                
                _logger.LogInformation($"? Klant delete events gepubliceerd naar RabbitMQ (Advanced): {klant.Naam} (ID: {klant.Id})");
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Fout bij publiceren delete event naar RabbitMQ: {ex.Message}");
            }
        });

        _logger.LogInformation($"Klant verwijderd: {klant.Naam} (ID: {klant.Id})");

        return NoContent();
    }
}
