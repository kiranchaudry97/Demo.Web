# ??? Delete Events via RabbitMQ - Complete Gids

## ?? Overzicht

Alle delete operaties (Klanten en Boeken verwijderen) worden nu via RabbitMQ gecommuniceerd naar externe systemen voor audit, archivering en synchronisatie.

---

## ?? Architecture

```
????????????????????????????????????????????????????????????????????
?                    DELETE EVENT FLOW                              ?
????????????????????????????????????????????????????????????????????

1. WEB INTERFACE
   ? (DELETE /api/klanten/{id} of /api/boeken/{id})
   
2. CONTROLLER (Producer)
   ?
   ??? Validatie:
   ?   ?? Bestaat entity?
   ?   ?? Kan verwijderd worden? (geen dependencies)
   ?
   ??? Create Delete Messages:
   ?   ?? KlantDeletedMessage / BoekDeletedMessage
   ?   ?? EntityChangeMessage (algemeen audit)
   ?
   ??? Delete from Database
   ?
   ??? Publish naar RabbitMQ (3 queues):
       ??? Queue: "klant_deleted" of "boek_deleted"
       ??? Queue: "entity_changes" (alle wijzigingen)
       
3. RABBITMQ BROKER
   ? Multiple Queues:
   ? - klant_deleted: Specifiek voor klant verwijderingen
   ? - boek_deleted: Specifiek voor boek verwijderingen
   ? - entity_changes: Algemene audit trail
   ?
   ?
   
4. ENTITY CHANGE CONSUMER (Background Service)
   ? 3 Consumers luisteren naar 3 queues
   ?
   ??? klant_deleted consumer:
   ?   ?? Log klant verwijdering
   ?   ?? Archive klant data
   ?   ?? Update externe systemen
   ?   ?? GDPR compliance rapportage
   ?
   ??? boek_deleted consumer:
   ?   ?? Log boek verwijdering
   ?   ?? Archive boek data
   ?   ?? Update inventory systemen
   ?   ?? Remove from search index
   ?
   ??? entity_changes consumer:
       ?? Centrale audit trail voor alle wijzigingen
```

---

## ?? Message Types

### 1. **KlantDeletedMessage**

```csharp
public class KlantDeletedMessage
{
    public int KlantId { get; set; }
    public string KlantNaam { get; set; }
    public string Email { get; set; }
    public DateTime DeletedAt { get; set; }
    public string Reason { get; set; }
}
```

**JSON Example:**
```json
{
  "klantId": 5,
  "klantNaam": "Maria de Vries",
  "email": "maria@example.com",
  "deletedAt": "2024-01-15T14:30:00Z",
  "reason": "User requested deletion via API"
}
```

---

### 2. **BoekDeletedMessage**

```csharp
public class BoekDeletedMessage
{
    public int BoekId { get; set; }
    public string Titel { get; set; }
    public string ISBN { get; set; }
    public int LaatsteVoorraad { get; set; }
    public DateTime DeletedAt { get; set; }
    public string Reason { get; set; }
}
```

**JSON Example:**
```json
{
  "boekId": 11,
  "titel": "Test Boek",
  "isbn": "978-1234567890",
  "laatsteVoorraad": 5,
  "deletedAt": "2024-01-15T14:35:00Z",
  "reason": "User requested deletion via API"
}
```

---

### 3. **EntityChangeMessage** (Algemeen)

```csharp
public class EntityChangeMessage
{
    public EntityType EntityType { get; set; }  // Klant, Boek, Order
    public ActionType Action { get; set; }      // Created, Updated, Deleted
    public int EntityId { get; set; }
    public string EntityName { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}
```

**JSON Example:**
```json
{
  "entityType": "Klant",
  "action": "Deleted",
  "entityId": 5,
  "entityName": "Maria de Vries",
  "timestamp": "2024-01-15T14:30:00Z",
  "data": {
    "email": "maria@example.com",
    "telefoon": "0612345678"
  }
}
```

---

## ?? RabbitMQ Queues

| Queue Name | Doel | Message Type | Consumer |
|-----------|------|--------------|----------|
| **klant_deleted** | Klant verwijderingen | KlantDeletedMessage | EntityChangeConsumer |
| **boek_deleted** | Boek verwijderingen | BoekDeletedMessage | EntityChangeConsumer |
| **entity_changes** | Alle wijzigingen | EntityChangeMessage | EntityChangeConsumer |
| **salesforce_orders** | Order creatie | OrderMessage | RabbitMqConsumer |

---

## ?? Validatie Rules

### **Klant Verwijderen**

```csharp
// Check if klant has orders
var hasOrders = await _context.Orders.AnyAsync(o => o.KlantId == id);
if (hasOrders)
{
    return BadRequest("Kan klant niet verwijderen. Er zijn nog orders gekoppeld.");
}
```

**Error Response:**
```json
{
  "error": "Kan klant niet verwijderen. Er zijn nog orders gekoppeld aan deze klant."
}
```

---

### **Boek Verwijderen**

```csharp
// Check if boek is used in orders
var usedInOrders = await _context.OrderRegels.AnyAsync(or => or.BoekId == id);
if (usedInOrders)
{
    return BadRequest("Kan boek niet verwijderen. Het boek is gebruikt in bestellingen.");
}
```

**Error Response:**
```json
{
  "error": "Kan boek niet verwijderen. Het boek is gebruikt in bestellingen."
}
```

---

## ?? Testing

### **Test 1: Klant Verwijderen**

```powershell
# DELETE request
curl -X DELETE http://localhost:5269/api/klanten/4 `
  -H "X-API-Key: demo-api-key-12345"
```

**Expected Logs:**

**Controller (Producer):**
```
info: Klant verwijderd: Test Klant (ID: 4)
info: Klant delete event gepubliceerd naar RabbitMQ: Test Klant (ID: 4)
```

**Consumer:**
```
info: Klant Verwijderd: Test Klant (ID: 4, Email: test@example.com) - Reason: User requested deletion via API
info: Entity Change: Deleted Klant 'Test Klant' (ID: 4) at 2024-01-15T14:30:00Z
```

---

### **Test 2: Boek Verwijderen**

```powershell
# DELETE request
curl -X DELETE http://localhost:5269/api/boeken/11 `
  -H "X-API-Key: demo-api-key-12345"
```

**Expected Logs:**

**Controller (Producer):**
```
info: Boek verwijderd: Test Boek (ID: 11)
info: Boek delete event gepubliceerd naar RabbitMQ: Test Boek (ID: 11)
```

**Consumer:**
```
info: Boek Verwijderd: Test Boek (ID: 11, ISBN: 978-1234567890, Laatste Voorraad: 10) - Reason: User requested deletion via API
info: Entity Change: Deleted Boek 'Test Boek' (ID: 11) at 2024-01-15T14:35:00Z
```

---

### **Test 3: Poging om Klant met Orders te Verwijderen**

```powershell
# Probeer klant met orders te verwijderen
curl -X DELETE http://localhost:5269/api/klanten/1 `
  -H "X-API-Key: demo-api-key-12345"
```

**Expected Response (400 Bad Request):**
```json
{
  "error": "Kan klant niet verwijderen. Er zijn nog orders gekoppeld aan deze klant."
}
```

**Geen RabbitMQ berichten!** (Validatie gefaald)

---

## ?? RabbitMQ Management UI

### **Check Queues**

Open: **http://localhost:15672**

Ga naar **Queues** tab:

| Queue | Consumers | Messages Ready | Message Rate |
|-------|-----------|----------------|--------------|
| klant_deleted | 1 | 0 | 0.5/s |
| boek_deleted | 1 | 0 | 0.3/s |
| entity_changes | 1 | 0 | 0.8/s |
| salesforce_orders | 1 | 0 | 1.2/s |

---

## ?? Use Cases voor Delete Events

### **1. Audit Trail**

Alle delete operaties worden gelogd in `entity_changes` queue:

```csharp
// Consumer schrijft naar audit database
await _auditService.LogDeletion(entityChange);
```

---

### **2. GDPR Compliance**

Bij klant verwijdering:

```csharp
// Consumer:
- Archive klant data (voor compliance)
- Notify data protection officer
- Update GDPR register
- Send confirmation email
```

---

### **3. Data Warehouse Sync**

```csharp
// Consumer stuurt naar data warehouse
await _dataWarehouseService.ArchiveDeletedEntity(klantDeleted);
```

---

### **4. External System Notifications**

```csharp
// Consumer notificeert externe systemen
await _crmService.RemoveCustomer(klantDeleted.KlantId);
await _mailingService.UnsubscribeEmail(klantDeleted.Email);
await _analyticsService.UpdateMetrics(klantDeleted);
```

---

### **5. Search Index Update**

```csharp
// Consumer update search indices
await _searchService.RemoveFromIndex("klanten", klantDeleted.KlantId);
await _elasticsearchService.DeleteDocument("boeken", boekDeleted.BoekId);
```

---

## ?? Complete Flow Example

### **Scenario: Verkoper verwijdert boek "Test Book"**

```
1. Web Interface
   ?? User klikt "Verwijderen" bij boek ID 11
      ?
2. BoekenController
   ?? Find boek in database
   ?? Check: Gebruikt in orders? ? Nee
   ?? Create BoekDeletedMessage
   ?? Create EntityChangeMessage
   ?? Delete from database
   ?? Publish to RabbitMQ
      ??? Queue: boek_deleted
      ??? Queue: entity_changes
      ?
3. RabbitMQ Broker
   ?? boek_deleted queue: 1 message
   ?? entity_changes queue: 1 message
      ?
4. EntityChangeConsumer
   ?? boek_deleted consumer:
   ?  ?? Deserialize message
   ?  ?? Log: "Boek Verwijderd: Test Book..."
   ?  ?? Archive boek data
   ?  ?? Update inventory system
   ?  ?? Remove from search index
   ?  ?? ACK message
   ?
   ?? entity_changes consumer:
      ?? Deserialize message
      ?? Log: "Entity Change: Deleted Boek..."
      ?? Write to audit database
      ?? ACK message
```

---

## ??? Configuration

### **appsettings.json** (geen wijzigingen nodig)

```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": "5672",
    "UserName": "guest",
    "Password": "guest"
  }
}
```

Alle queues worden automatisch aangemaakt!

---

## ?? Extending the System

### **Add Custom Processing**

In `EntityChangeConsumerService.cs`:

```csharp
// In StartKlantDeletedConsumer
consumer.Received += async (model, ea) =>
{
    var klantDeleted = JsonSerializer.Deserialize<KlantDeletedMessage>(message);
    
    // YOUR CUSTOM LOGIC HERE:
    await _emailService.SendGoodbyeEmail(klantDeleted.Email);
    await _crmService.RemoveContact(klantDeleted.KlantId);
    await _analyticsService.TrackChurn(klantDeleted);
    
    _channelKlantDeleted.BasicAck(ea.DeliveryTag, false);
};
```

---

## ?? Monitoring

### **Metrics to Track**

| Metric | Description | Alert Threshold |
|--------|-------------|-----------------|
| **Delete Rate** | Deletions per minute | > 10/min |
| **Queue Length** | Messages waiting | > 100 |
| **Consumer Lag** | Processing delay | > 5 seconds |
| **Failed ACKs** | Processing failures | > 5% |

---

## ?? Summary

| Feature | Status | Description |
|---------|--------|-------------|
| **Klant Delete** | ? | Met RabbitMQ event |
| **Boek Delete** | ? | Met RabbitMQ event |
| **Validation** | ? | Check dependencies |
| **3 Queues** | ? | Specifiek + Algemeen |
| **3 Consumers** | ? | Dedicated processing |
| **Audit Trail** | ? | entity_changes queue |
| **Error Handling** | ? | NACK + Requeue |
| **Logging** | ? | Alle events gelogd |

---

## ? Checklist

- ? KlantDeletedMessage model
- ? BoekDeletedMessage model
- ? EntityChangeMessage model
- ? RabbitMqService uitgebreid (4 queues)
- ? KlantenController met delete event
- ? BoekenController met delete event
- ? EntityChangeConsumerService (3 consumers)
- ? Validatie van dependencies
- ? Async publishing (non-blocking)
- ? Comprehensive logging
- ? Documentation

---

**Delete events via RabbitMQ zijn volledig geïmplementeerd! ????**
