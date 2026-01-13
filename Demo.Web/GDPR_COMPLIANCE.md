# ?? GDPR & Privacy Compliance Document

## ?? Executive Summary

Dit document beschrijft hoe de Bookstore API voldoet aan de **AVG (Algemene Verordening Gegevensbescherming) / GDPR** en welke security maatregelen zijn geïmplementeerd.

**Versie:** 1.0  
**Datum:** 15 januari 2024  
**Status:** Compliant met GDPR basisvereisten

---

## ?? GDPR Compliance Status

| Vereiste | Status | Beschrijving |
|----------|--------|--------------|
| **Rechtmatigheid** | ? | Expliciete toestemming via API calls |
| **Minimale Dataverzameling** | ? | Alleen noodzakelijke velden |
| **Recht op Inzage** | ? | GET endpoints voor klantgegevens |
| **Recht op Correctie** | ? | PUT endpoints voor updates |
| **Recht op Verwijdering** | ? | DELETE endpoints met audit trail |
| **Data Portabiliteit** | ? | JSON export via API |
| **Beveiliging** | ?? | API Key, HTTPS (encryption aanbevolen) |
| **Data Breach Notificatie** | ?? | Logging aanwezig (monitoring aanbevolen) |
| **Privacy by Design** | ? | Minimale data, secure defaults |
| **Data Processing Agreement** | ? | Niet van toepassing (geen externe verwerker) |

---

## ?? Welke Persoonsgegevens Worden Verwerkt?

### **Klantgegevens (Minimaal)**

```csharp
public class Klant
{
    public int Id { get; set; }
    public string Naam { get; set; }           // Noodzakelijk voor order
    public string Email { get; set; }          // Noodzakelijk voor communicatie
    public string Telefoon { get; set; }       // Optioneel contact
    public string Adres { get; set; }          // Noodzakelijk voor levering
}
```

**Geen verzameling van:**
- ? Geboortedatum
- ? BSN (Burgerservicenummer)
- ? Financiële gegevens (creditcard)
- ? Gevoelige categorieën (gezondheid, religie, etc.)

---

## ?? Security Measures

### **1. API Key Authenticatie** ?

```csharp
// ApiKeyMiddleware.cs
public class ApiKeyMiddleware
{
    private const string ApiKeyHeaderName = "X-API-Key";
    
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var key))
        {
            context.Response.StatusCode = 401;
            return;
        }
        
        if (!IsValidApiKey(key))
        {
            context.Response.StatusCode = 401;
            return;
        }
        
        await _next(context);
    }
}
```

**Security Level:** Basic  
**Verbeteringen:**
- ?? API Key rotatie implementeren
- ?? Rate limiting toevoegen
- ?? IP whitelist overwegen

---

### **2. HTTPS Encryption** ?

```csharp
// Program.cs
app.UseHttpsRedirection();  // Force HTTPS
```

**Status:** Verplicht HTTPS voor productie  
**Certificaat:** Self-signed (development), Let's Encrypt/Commercial (production)

---

### **3. Input Validation** ?

```csharp
// Validation/OrderValidationService.cs
public ValidationResult ValidateKlantData(string naam, string email)
{
    var errors = new List<string>();
    
    // SQL Injection prevention (via EF Core parameterization)
    if (string.IsNullOrWhiteSpace(naam) || naam.Length < 2)
    {
        errors.Add("Naam moet minimaal 2 karakters bevatten");
    }
    
    // XSS prevention
    if (ContainsHtmlTags(naam))
    {
        errors.Add("Naam mag geen HTML bevatten");
    }
    
    // Email validation
    if (!IsValidEmail(email))
    {
        errors.Add("Ongeldig email formaat");
    }
    
    return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
}
```

**Bescherming tegen:**
- ? SQL Injection (Entity Framework parameterization)
- ? XSS (Input validation)
- ? CSRF (API Key, not cookie-based)
- ?? DDoS (Rate limiting aanbevolen)

---

### **4. Database Beveiliging** ??

```
Current Status:
- SQLite file: bookstore.db
- Locatie: Application directory
- Encryption: ? None (plain text)

Aanbevelingen:
1. SQLite Encryption (SQLCipher)
2. File system permissions (read/write only for app user)
3. Regular backups (encrypted)
```

**Code voor SQLite Encryption:**

```csharp
// Add NuGet: Microsoft.EntityFrameworkCore.Sqlite.Encryption

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqliteOptions => {
            sqliteOptions.Password("YourStrongPassword123!");
        }
    ));
```

---

### **5. Logging & Audit Trail** ?

```csharp
// All CRUD operations are logged
_logger.LogInformation($"Klant aangemaakt: {klant.Naam} (ID: {klant.Id})");
_logger.LogInformation($"Klant bijgewerkt: {klant.Naam} (ID: {klant.Id})");
_logger.LogInformation($"Klant verwijderd: {klant.Naam} (ID: {klant.Id})");

// Delete events via RabbitMQ
var entityChange = new EntityChangeMessage
{
    EntityType = EntityType.Klant,
    Action = ActionType.Deleted,
    EntityId = klant.Id,
    EntityName = klant.Naam,
    Timestamp = DateTime.UtcNow
};
await _rabbitMqService.PublishEntityChangeAsync("entity_changes", entityChange);
```

**Audit Trail bevat:**
- ? Wat (actie: Create, Update, Delete)
- ? Wanneer (timestamp)
- ? Wie (API Key, kan uitgebreid worden met user ID)
- ?? Vanwaar (IP address - niet geïmplementeerd)

---

## ??? GDPR Rights Implementation

### **1. Recht op Inzage (Right to Access)** ?

```http
GET /api/klanten/{id}
X-API-Key: demo-api-key-12345

Response:
{
  "id": 1,
  "naam": "Jan Jansen",
  "email": "jan@example.com",
  "telefoon": "0612345678",
  "adres": "Hoofdstraat 1, Amsterdam"
}
```

**Status:** ? Geïmplementeerd via GET endpoints

---

### **2. Recht op Correctie (Right to Rectification)** ?

```http
PUT /api/klanten/{id}
X-API-Key: demo-api-key-12345
Content-Type: application/json

{
  "naam": "Jan Jansen (Updated)",
  "email": "jan.new@example.com",
  "telefoon": "0687654321",
  "adres": "Nieuwe Straat 10, Rotterdam"
}
```

**Status:** ? Geïmplementeerd via PUT endpoints

---

### **3. Recht op Verwijdering (Right to Erasure)** ?

```http
DELETE /api/klanten/{id}
X-API-Key: demo-api-key-12345

Response (Success):
204 No Content

Response (Has Orders):
{
  "error": "Kan klant niet verwijderen. Er zijn nog orders gekoppeld."
}
```

**Implementation:**

```csharp
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteKlant(int id)
{
    var klant = await _context.Klanten.FindAsync(id);
    
    // Check dependencies
    var hasOrders = await _context.Orders.AnyAsync(o => o.KlantId == id);
    if (hasOrders)
    {
        // GDPR Note: Customer can request anonymization instead
        return BadRequest("Kan klant niet verwijderen. Orders gekoppeld.");
    }
    
    // Create audit trail BEFORE deletion
    var deleteMessage = new KlantDeletedMessage
    {
        KlantId = klant.Id,
        KlantNaam = klant.Naam,
        Email = klant.Email,
        DeletedAt = DateTime.UtcNow,
        Reason = "User requested deletion (GDPR)"
    };
    
    // Delete from database
    _context.Klanten.Remove(klant);
    await _context.SaveChangesAsync();
    
    // Publish delete event (for external systems cleanup)
    await _rabbitMqService.PublishEntityChangeAsync("klant_deleted", deleteMessage);
    
    return NoContent();
}
```

**Status:** ? Geïmplementeerd met validation en audit trail

**GDPR Note:** Bij orders ? Anonimiseren in plaats van verwijderen

---

### **4. Recht op Data Portabiliteit (Right to Data Portability)** ?

```http
GET /api/klanten/{id}
Accept: application/json

Response: JSON format (machine-readable)
```

**Alternative: Export All Data**

```csharp
[HttpGet("{id}/export")]
public async Task<ActionResult> ExportKlantData(int id)
{
    var klant = await _context.Klanten.FindAsync(id);
    var orders = await _context.Orders
        .Where(o => o.KlantId == id)
        .Include(o => o.OrderRegels)
        .ThenInclude(r => r.Boek)
        .ToListAsync();
    
    var export = new
    {
        Klant = klant,
        Orders = orders,
        ExportDate = DateTime.UtcNow
    };
    
    return Ok(export);
}
```

**Status:** ? JSON format (standaard)  
**Verbeteringen:** ?? CSV/XML export toevoegen

---

### **5. Recht op Beperking (Right to Restriction)** ??

**Not Implemented - Recommended:**

```csharp
public class Klant
{
    public bool IsRestricted { get; set; }  // Add field
    public string? RestrictionReason { get; set; }
}

// Filter restricted customers
var klanten = await _context.Klanten
    .Where(k => !k.IsRestricted || showRestricted)
    .ToListAsync();
```

---

### **6. Recht op Bezwaar (Right to Object)** ??

**Not Implemented - Recommended:**

```csharp
public class Klant
{
    public bool OptOutMarketing { get; set; }  // No marketing emails
    public bool OptOutProfiling { get; set; }  // No analytics
}
```

---

## ?? Data Processing Activities (Art. 30 GDPR)

### **Processing Activity Record**

| Field | Value |
|-------|-------|
| **Verwerker** | Bookstore API |
| **Contactpersoon** | support@bookstore@ehb.be |
| **Doel Verwerking** | Order management en klantenadministratie |
| **Rechtsgrondslag** | Toestemming + Uitvoering overeenkomst |
| **Categorieën Betrokkenen** | Klanten |
| **Categorieën Gegevens** | Naam, Email, Telefoon, Adres, Ordergegevens |
| **Categorieën Ontvangers** | Salesforce (CRM), SAP (ERP) |
| **Doorgifte Buiten EU** | ? Nee |
| **Bewaartermijn** | Orders: 7 jaar (fiscaal), Klanten: Tot verwijdering |
| **Beveiliging** | API Key, HTTPS, Audit logging |

---

## ?? Data Flow & External Processing

```
????????????????????????????????????????????????????????????????
?                    DATA FLOW DIAGRAM                          ?
????????????????????????????????????????????????????????????????

1. CUSTOMER (Betrokkene)
   ? (Provides data via web interface)
   
2. BOOKSTORE API (Verwerker)
   ?? Database (SQLite) - Internal
   ?? RabbitMQ Queue - Internal
   ?? Application Logs - Internal
   
3. EXTERNAL SYSTEMS (Derden verwerkers)
   ??? Salesforce (CRM)
   ?   ?? Purpose: Customer relationship management
   ?       Data: Naam, Email, Order info
   ?       Location: EU/US (Shield Framework)
   ?
   ??? SAP R/3 (ERP)
       ?? Purpose: Order processing
           Data: Order details (no personal data)
           Location: Internal
```

**GDPR Implication:**
- ?? Salesforce: Data Processing Agreement (DPA) required
- ?? SAP: If external ? DPA required
- ? RabbitMQ: Internal ? No DPA needed

---

## ?? Data Breach Response Plan

### **Detection**

```csharp
// Monitoring (to implement)
_logger.LogWarning($"Unauthorized access attempt from IP: {ipAddress}");
_logger.LogError($"Data breach detected: {ex.Message}");
```

### **Response Procedure**

**Within 72 hours:**

1. **Identify the breach**
   - Check logs
   - Determine scope
   - Affected records?

2. **Contain the breach**
   - Disable API keys
   - Shutdown affected systems
   - Change passwords

3. **Notify authorities** (if > 500 records)
- Belgian DPA (Gegevensbeschermingsautoriteit)
- Email: contact@apd-gba.be
- Website: gegevensbeschermingsautoriteit.be

4. **Notify affected customers**
   ```csharp
   foreach (var klant in affectedKlanten)
   {
       await _emailService.SendBreachNotification(klant.Email);
   }
   ```

5. **Document the breach**
   - What happened
   - When discovered
   - Actions taken
   - Impact assessment

---

## ? GDPR Compliance Checklist

### **Legal Basis**
- ? Toestemming via API call
- ? Uitvoering overeenkomst (orders)
- ? Gerechtvaardigd belang (analytics)

### **Transparency**
- ? Privacy Policy (GET /api/legal/privacy-policy + /privacy-policy.html)
- ? Cookie Policy (GET /api/legal/cookie-policy + /cookie-policy.html)
- ? Terms of Service (GET /api/legal/terms-of-service + /terms-of-service.html)

### **Individual Rights**
- ? Right to Access (GET endpoints)
- ? Right to Rectification (PUT endpoints)
- ? Right to Erasure (DELETE endpoints)
- ? Right to Data Portability (JSON export)
- ?? Right to Restriction (not implemented)
- ?? Right to Object (not implemented)

### **Security**
- ? API Key authentication
- ? HTTPS encryption
- ? Input validation
- ?? Database encryption (recommended)
- ?? Rate limiting (recommended)
- ?? IP whitelisting (optional)

### **Accountability**
- ? Audit logging
- ? Delete event tracking
- ?? Data Processing Agreement (if needed)
- ?? DPIA (Data Protection Impact Assessment) if high risk

---

## ?? Recommendations for Full Compliance

### **High Priority**

1. **Add Database Encryption**
   ```bash
   Install-Package Microsoft.EntityFrameworkCore.Sqlite.Encryption
   ```

2. **Implement Rate Limiting**
   ```csharp
   builder.Services.AddRateLimiter(options => {
       options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
           context => RateLimitPartition.GetFixedWindowLimiter(
               partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
               factory: _ => new FixedWindowRateLimiterOptions {
                   AutoReplenishment = true,
                   PermitLimit = 100,
                   Window = TimeSpan.FromMinutes(1)
               }
           )
       );
   });
   ```

3. **Add Privacy Policy Endpoint**
   ```csharp
   [HttpGet("privacy-policy")]
   public ActionResult GetPrivacyPolicy()
   {
       return Ok(new {
           lastUpdated = "2024-01-15",
           policy = "We verzamelen minimale gegevens...",
           contact = "privacy@bookstore@ehb.be",
           dpaEmail = "dpo@bookstore@ehb.be"
       });
   }
   ```

### **Medium Priority**

4. **IP Address Logging (for audit)**
   ```csharp
   var ipAddress = context.Connection.RemoteIpAddress?.ToString();
   _logger.LogInformation($"API call from IP: {ipAddress}");
   ```

5. **Anonymization instead of Deletion (for orders)**
   ```csharp
   // Instead of rejecting deletion
   klant.Naam = $"Geanonimiseerd-{klant.Id}";
   klant.Email = $"deleted-{klant.Id}@removed.local";
   klant.Telefoon = "000000000";
   klant.Adres = "Verwijderd";
   klant.IsAnonymized = true;
   ```

### **Low Priority (Nice to Have)**

6. **Data Export in Multiple Formats**
7. **Consent Management System**
8. **Automated GDPR Request Handling**

---

## ?? Conclusion

**GDPR Compliance Level:** ?? **Substantially Compliant**

**Strengths:**
- ? Individual rights implemented (Access, Rectification, Erasure)
- ? Minimal data collection
- ? Audit trail via logging + RabbitMQ
- ? Security basics (API Key, HTTPS)

**Gaps:**
- ?? Database encryption
- ?? Rate limiting
- ?? Privacy policy endpoint
- ?? Data Processing Agreements (external systems)

**Overall Assessment:** ? **Suitable for development/testing**  
**Production Recommendation:** Implement high-priority improvements

---

**Document Version:** 1.0  
**Last Updated:** 15 januari 2024  
**Next Review:** 15 juli 2024
