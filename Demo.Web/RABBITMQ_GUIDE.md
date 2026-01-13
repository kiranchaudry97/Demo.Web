# ?? RabbitMQ Integratie - Complete Uitleg

## ?? Overzicht

Deze applicatie gebruikt **RabbitMQ** voor asynchrone messaging tussen de Web API en Salesforce.

---

## ?? Architecture Flow

```
????????????????????????????????????????????????????????????????????
?                      ORDER PROCESSING FLOW                        ?
????????????????????????????????????????????????????????????????????

1. WEB INTERFACE
   ? (HTTP POST /api/orders)
   
2. ORDERS CONTROLLER (Publisher/Producer)
   ?
   ??? Database: Order opslaan
   ?
   ??? PUBLISHER: Bericht naar RabbitMQ
   ?   ?
   ?   ??? Queue: "salesforce_orders"
   ?       ? Message Format: OrderMessage JSON
   ?       ? {
   ?       ?   "orderNummer": "ORD20240115...",
   ?       ?   "klantNaam": "Jan Jansen",
   ?       ?   "klantEmail": "jan@example.com",
   ?       ?   "orderDatum": "2024-01-15T12:30:00Z",
   ?       ?   "totaalBedrag": 99.98,
   ?       ?   "items": [ ... ]
   ?       ? }
   ?       ?
   ?       ? Properties:
   ?       ? - Durable: true (blijft bij restart)
   ?       ? - Persistent: true (niet verloren bij crash)
   ?       ? - ContentType: application/json
   ?       ?
   ?       ?
   ?   
   3. RABBITMQ BROKER
      ? Queue: salesforce_orders
      ? - Messages wachten tot ze worden verwerkt
      ? - Consumer kan ze op eigen tempo lezen
      ? - Automatic retry bij fouten
      ?
      ?
   
   4. CONSUMER SERVICE (Background Service)
      ? Luistert continu naar nieuwe berichten
      ?
      ??? Bericht ontvangen
      ?   ?
      ?   ??? Deserialize JSON ? OrderMessage
      ?   ?
      ?   ??? Roep Salesforce Service aan
      ?   ?   ??? CreateOrderAsync(orderMessage)
      ?   ?
      ?   ??? SUCCESS:
      ?   ?   ??? BasicAck (bevestig bericht)
      ?   ?       Log: "Order verwerkt: SF123456"
      ?   ?
      ?   ??? FAILURE:
      ?       ??? BasicNack + Requeue
      ?           Log: "Fout, opnieuw proberen"
      ?
      ?
   
   5. SALESFORCE
      Order aangemaakt in CRM
      ID toegewezen: SF12345678
```

---

## ?? Components

### 1. **OrderMessage Model** (`Models/OrderMessage.cs`)

```csharp
public class OrderMessage
{
    public string OrderNummer { get; set; }
    public string KlantNaam { get; set; }
    public string KlantEmail { get; set; }
    public DateTime OrderDatum { get; set; }
    public decimal TotaalBedrag { get; set; }
    public List<OrderItemMessage> Items { get; set; }
}
```

**Doel:** Gestandaardiseerde message structuur voor RabbitMQ

---

### 2. **RabbitMQ Service (Producer)** (`Services/RabbitMqService.cs`)

**Rol:** Publisher - Verstuurt berichten naar queue

```csharp
public async Task PublishOrderToSalesforceAsync(object orderData)
{
    var message = JsonSerializer.Serialize(orderData);
    var body = Encoding.UTF8.GetBytes(message);
    
    var properties = _channel.CreateBasicProperties();
    properties.Persistent = true; // ? Overleeft RabbitMQ restart
    
    _channel.BasicPublish(
        exchange: string.Empty,
        routingKey: "salesforce_orders",
        basicProperties: properties,
        body: body
    );
}
```

**Features:**
- ? Singleton service (1 connectie)
- ? Persistent messages
- ? Durable queue
- ? Fallback mode (werkt zonder RabbitMQ)

---

### 3. **RabbitMQ Consumer Service** (`Services/RabbitMqConsumerService.cs`)

**Rol:** Consumer - Leest berichten van queue

```csharp
public class RabbitMqConsumerService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var orderMessage = JsonSerializer.Deserialize<OrderMessage>(message);
            
            await ProcessOrderMessageAsync(orderMessage);
            
            _channel.BasicAck(ea.DeliveryTag, false); // ? Bevestig verwerking
        };
        
        _channel.BasicConsume("salesforce_orders", autoAck: false, consumer);
    }
}
```

**Features:**
- ? Background Service (draait continu)
- ? Async message processing
- ? Manual ACK (bevestiging)
- ? Automatic retry bij fouten
- ? Logging van alle acties

---

## ?? Message Flow Patterns

### **Producer ? Queue ? Consumer Pattern**

```
[Web API]  ?  [Publisher]  ?  [RabbitMQ Queue]  ?  [Consumer]  ?  [Salesforce]
   POST          Publish          Store/Wait         Process        Create Order
   /orders       Message          Messages           Message        Return ID
```

**Voordelen:**
- ?? **Asynchronous:** API reageert direct, verwerking in achtergrond
- ?? **Resilient:** Messages blijven bij crash
- ? **Scalable:** Meerdere consumers mogelijk
- ?? **Decoupled:** API en Salesforce kennen elkaar niet

---

## ?? Configuration

### **appsettings.json**

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

### **Queue Settings**

| Setting | Value | Beschrijving |
|---------|-------|--------------|
| **Name** | salesforce_orders | Queue naam |
| **Durable** | true | Blijft bestaan bij restart |
| **Exclusive** | false | Meerdere consumers toegestaan |
| **AutoDelete** | false | Queue blijft actief zonder consumers |
| **Persistent** | true | Messages overleven restart |

---

## ?? Message Acknowledgment

### **ACK (Acknowledge)**

```csharp
_channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
```

**Betekenis:** ? "Bericht succesvol verwerkt, verwijder uit queue"

### **NACK (Negative Acknowledge)**

```csharp
_channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
```

**Betekenis:** ? "Bericht mislukt, zet terug in queue voor nieuwe poging"

---

## ?? Testen

### **1. Check RabbitMQ Management**

Open: http://localhost:15672
- Username: `guest`
- Password: `guest`

**Ga naar:** Queues ? `salesforce_orders`

### **2. Plaats Order via API**

```powershell
# POST request naar API
curl -X POST http://localhost:5269/api/orders `
  -H "X-API-Key: demo-api-key-12345" `
  -H "Content-Type: application/json" `
  -d '{"klantId":1,"items":[{"boekId":1,"aantal":2}]}'
```

### **3. Bekijk Logs**

**Producer logs:**
```
info: Order gepubliceerd naar RabbitMQ queue: salesforce_orders
```

**Consumer logs:**
```
info: Bericht ontvangen van RabbitMQ: {"orderNummer":"ORD...","klantNaam":"Jan Jansen"...}
info: Versturen naar Salesforce: Order ORD20240115... voor klant Jan Jansen
info: Order ORD20240115... succesvol aangemaakt in Salesforce met ID: SF12345678
info: Bericht succesvol verwerkt en bevestigd: ORD20240115...
```

### **4. Check Queue in Management UI**

- **Messages Ready:** 0 (alle verwerkt)
- **Consumers:** 1 (jouw consumer)
- **Message Rate:** Aantal berichten per seconde

---

## ?? Retry Logic

### **Bij Fout in Consumer:**

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Fout bij verwerken bericht");
    _channel.BasicNack(ea.DeliveryTag, false, requeue: true); // ? Requeue!
}
```

**Wat gebeurt er:**
1. Bericht gaat terug naar queue
2. Consumer probeert opnieuw
3. Blijft proberen tot succesvol
4. Voorkomt data verlies

---

## ?? Scaling

### **Meerdere Consumers**

Je kunt meerdere instances van de applicatie draaien:

```
Instance 1: Consumer A } 
Instance 2: Consumer B }  ? Same Queue ? Load Balanced!
Instance 3: Consumer C } 
```

RabbitMQ verdeelt berichten automatisch over consumers.

---

## ?? Best Practices

### ? **DO:**
- Gebruik `BasicAck` alleen na **succesvol** verwerken
- Log alle acties voor debugging
- Gebruik `Persistent` messages
- Maak queue `Durable`
- Gebruik manual ACK (`autoAck: false`)

### ? **DON'T:**
- Nooit `autoAck: true` in productie
- Geen sensitive data in messages
- Geen eindeloze retry loops
- Niet vergeten om connection te sluiten

---

## ?? Monitoring

### **Wat te Monitoren:**

| Metric | Wat | Alarm bij |
|--------|-----|-----------|
| **Queue Length** | Aantal wachtende messages | > 1000 |
| **Consumer Count** | Aantal actieve consumers | = 0 |
| **Message Rate** | Messages per seconde | Plots 0 |
| **Unacked Messages** | Niet bevestigde messages | > 100 |
| **Error Rate** | Fouten in logs | > 5% |

---

## ?? Troubleshooting

### **Problem:** Consumer start niet

**Check:**
```powershell
# Check of RabbitMQ draait
docker ps | Select-String rabbitmq

# Check logs
docker logs rabbitmq
```

**Fix:**
```powershell
docker start rabbitmq
```

---

### **Problem:** Messages stapelen zich op

**Oorzaken:**
1. Consumer crashed
2. Processing duurt te lang
3. Salesforce niet bereikbaar

**Fix:**
- Check consumer logs
- Verhoog `BasicQos` prefetch count
- Start extra consumers

---

### **Problem:** Messages verdwijnen

**Oorzaken:**
1. `autoAck: true` gebruikt
2. Messages niet persistent
3. Queue niet durable

**Fix:**
- Gebruik manual ACK
- Set `Persistent: true`
- Set `Durable: true`

---

## ?? Message Examples

### **Order Message (JSON)**

```json
{
  "orderNummer": "ORD20240115123045",
  "klantNaam": "Jan Jansen",
  "klantEmail": "jan.jansen@example.com",
  "orderDatum": "2024-01-15T12:30:45Z",
  "totaalBedrag": 99.98,
  "items": [
    {
      "boekTitel": "C# in Depth",
      "aantal": 2,
      "prijs": 49.99
    }
  ]
}
```

---

## ?? Summary

| Component | Type | Rol |
|-----------|------|-----|
| **RabbitMqService** | Producer | Publiceert messages naar queue |
| **RabbitMqConsumerService** | Consumer | Verwerkt messages van queue |
| **OrderMessage** | Model | Gestandaardiseerd bericht formaat |
| **Queue: salesforce_orders** | Storage | Buffert messages |
| **Salesforce Service** | External | Verwerkt order in CRM |

---

**RabbitMQ is nu volledig geïntegreerd met Producer/Consumer pattern! ??**
