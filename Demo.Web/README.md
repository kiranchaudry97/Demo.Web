# Bookstore API - Demo Applicatie

## ?? Overzicht
Complete ASP.NET Core Web API voor boekverkoop met moderne web interface en integratie voor RabbitMQ, SAP iDoc en Salesforce.

## ? Functionaliteiten
? **Web Interface** - Complete beheerpagina met alle functionaliteiten op één pagina  
? **API Key Authenticatie** - Beveiligde endpoints met X-API-Key header  
? **SQLite Database** - Lokale database voor klanten, boeken en orders  
? **RabbitMQ Integratie** - Asynchrone messaging naar Salesforce  
? **SAP iDoc** - XML transformatie naar SAP ORDERS05 formaat  
? **CRUD Operaties** - Volledig beheer van klanten en boeken  
? **Voorraad Management** - Automatische voorraad controle en update  
? **Winkelmandje** - Meerdere boeken bestellen in één order  
? **Parallel Processing** - Gelijktijdige verwerking naar RabbitMQ en SAP  

## ??? Technische Stack
- .NET 9.0
- ASP.NET Core Web API
- Entity Framework Core met SQLite
- RabbitMQ.Client 6.8.1
- Vanilla JavaScript frontend
- Swagger/OpenAPI documentatie

## ?? Quick Start

### 1. Installeer Dependencies
```bash
dotnet restore
```

### 2. Database Setup
De database wordt automatisch aangemaakt bij eerste start met seed data:
- **3 Klanten**: Jan Jansen, Piet Pietersen, Test Gebruiker
- **10 Boeken**: C# in Depth, Clean Code, Design Patterns, en meer...

### 3. RabbitMQ (Optioneel)
Voor RabbitMQ functionaliteit, installeer Docker en run:
```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```
Of pas de settings aan in `appsettings.json`.

**Note**: De API werkt ook zonder RabbitMQ (fallback mode).


### 4. Start de Applicatie
```bash
dotnet run
```

De applicatie is beschikbaar op: `http://localhost:5269` of `https://localhost:7200`

## ?? Web Interface

**Open je browser:** `http://localhost:5269` (of `https://localhost:7200`)

De web interface toont alles op één pagina (zoals het ontwerp):

### ?? Bestellingen Sectie
- Overzicht van alle geplaatste orders
- Klantinformatie en totaalbedrag per order
- **Details** knop toont: Salesforce ID, SAP status, items, en meer

### ?? Klantenbeheer Sectie
- Tabel met alle klanten (naam + email)
- **Bewerken** - Klant aanpassen via modal
- **Verwijderen** - Klant verwijderen (met bevestiging)
- **Nieuwe Klant** knop - Klant toevoegen

### ?? Klant Details Sectie
Formulier voor het beheren van klanten:
- Naam, Email, Telefoon, Adres velden
- Gebruik voor nieuwe klanten of bewerk bestaande
- **Opslaan** knop om wijzigingen op te slaan

### ?? Boeken Sectie
Tabel met **10 boeken** inclusief:
- Titel en Auteur
- Prijs (€)
- **Voorraad aantal** (met waarschuwing bij < 15)
- ISBN nummer
- **Bewerken** en **Verwijderen** knoppen

### ?? Bestel een Boek Sectie
Complete order functionaliteit:
1. **Klant selecteren** - Dropdown met alle klanten
2. **Boek selecteren** - Dropdown toont titel, prijs EN voorraad
3. **Aantal kiezen** - Input veld voor hoeveelheid
4. **Toevoegen aan winkelmandje** - Knop om toe te voegen
5. **Winkelmandje** - Overzicht met:
   - Alle geselecteerde boeken
   - Aantal per boek
   - Subtotaal per regel
   - **Totaalbedrag** onderaan
   - **Verwijderen (×)** knop per item
6. **Bestellen** knop - Plaatst order met:
   - RabbitMQ publicatie naar Salesforce
   - SAP iDoc generatie en verwerking
   - Order bevestiging met beide statussen

## ?? API Endpoints (voor ontwikkelaars)


**Alle endpoints vereisen een API Key header:**
```
X-API-Key: demo-api-key-12345
```

## API Endpoints

### Klanten
- `GET /api/klanten` - Alle klanten ophalen
- `GET /api/klanten/{id}` - Specifieke klant ophalen
- `POST /api/klanten` - Nieuwe klant toevoegen
- `PUT /api/klanten/{id}` - Klant bijwerken
- `DELETE /api/klanten/{id}` - Klant verwijderen

### Boeken
- `GET /api/boeken` - Alle boeken ophalen
- `GET /api/boeken/{id}` - Specifiek boek ophalen
- `GET /api/boeken/{id}/voorraad` - Voorraad status
- `POST /api/boeken` - Nieuw boek toevoegen
- `PUT /api/boeken/{id}` - Boek bijwerken
- `DELETE /api/boeken/{id}` - Boek verwijderen

### Orders
- `GET /api/orders` - Alle orders ophalen
- `GET /api/orders/{id}` - Specifieke order ophalen
- `POST /api/orders` - Nieuwe order plaatsen

## Order Proces Flow

```
1. Web App ? HTTP POST /api/orders (met API Key)
2. Validatie: API Key, klant, boeken, voorraad
3. Order aanmaken in database
4. PARALLEL VERWERKING:
   ?? 3a) RabbitMQ ? Salesforce
   ?   ?? Publiceer naar queue 'salesforce_orders'
   ?   ?? Salesforce Consumer maakt order aan
   ?? 3b) SAP iDoc ? SAP R/3
       ?? JSON ? XML (ORDERS05 formaat)
       ?? Status 64: Klaar voor verwerking
       ?? Status 53: Succesvol verwerkt (51 = fout)
5. Response met beide resultaten
```

## Voorbeeld API Calls

### Order Plaatsen
```http
POST /api/orders
X-API-Key: demo-api-key-12345
Content-Type: application/json

{
  "klantId": 1,
  "items": [
    {
      "boekId": 1,
      "aantal": 2
    },
    {
      "boekId": 2,
      "aantal": 1
    }
  ]
}
```

### Response
```json
{
  "orderId": 1,
  "orderNummer": "ORD20240115123045",
  "status": "In behandeling",
  "totaalBedrag": 99.97,
  "orderDatum": "2024-01-15T12:30:45Z",
  "salesforceId": "SF12345678",
  "sapStatus": "Status 53: iDoc succesvol verwerkt",
  "bericht": "Order succesvol aangemaakt en verstuurd naar Salesforce (via RabbitMQ) en SAP"
}
```

### Klant Toevoegen
```http
POST /api/klanten
X-API-Key: demo-api-key-12345
Content-Type: application/json

{
  "naam": "Test Gebruiker",
  "email": "test@email.com",
  "telefoon": "0612345678",
  "adres": "Teststraat 1, Amsterdam"
}
```

### Voorraad Controleren
```http
GET /api/boeken/1/voorraad
X-API-Key: demo-api-key-12345
```

## Swagger UI
Open je browser en ga naar: `https://localhost:5001`

Hier vind je interactieve API documentatie waar je alle endpoints kunt testen.

**Let op**: Klik op "Authorize" en voer de API Key in: `demo-api-key-12345`

## Configuratie

### appsettings.json
```json
{
  "ApiKey": "demo-api-key-12345",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=bookstore.db"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": "5672",
    "UserName": "guest",
    "Password": "guest"
  }
}
```

## Logging
De applicatie logt alle belangrijke acties:
- Order creatie
- RabbitMQ publicatie
- SAP iDoc verwerking
- Salesforce integratie
- CRUD operaties op klanten en boeken

Check de console output voor real-time logs.

## Database Locatie
SQLite database: `Demo.Web/bookstore.db`

## SAP iDoc Formaat
De applicatie genereert XML in ORDERS05 formaat:
- **E1EDK01**: Header met BELNR (ordernummer)
- **E1EDKA1**: Klantgegevens
- **E1EDP01**: Order regels met artikeldata

## Loose Coupling
- Als RabbitMQ niet beschikbaar is, werkt de app in fallback mode
- Als SAP faalt, wordt Salesforce toch verwerkt (en vice versa)
- Beide systemen worden parallel verwerkt voor optimale performance

## Troubleshooting

### API Key fout
**Probleem**: `401 Unauthorized - API Key ontbreekt`  
**Oplossing**: Voeg de header toe: `X-API-Key: demo-api-key-12345`

### RabbitMQ verbinding mislukt
**Probleem**: `RabbitMQ verbinding mislukt`  
**Oplossing**: Dit is normaal als RabbitMQ niet draait. De app werkt in fallback mode.

### Onvoldoende voorraad
**Probleem**: `Onvoldoende voorraad voor 'Boek Titel'`  
**Oplossing**: Check beschikbare voorraad via `/api/boeken/{id}/voorraad`

## GDPR & Beveiliging
- ? API Key authenticatie
- ? Validatie van alle input
- ? Beveiligde HTTPS communicatie
- ? Logging van alle acties voor audit trail

## Ontwikkelaar Notities
- RabbitMQ en SAP zijn gesimuleerd voor demo doeleinden
- Salesforce integratie is een mock implementatie
- Voor productie: implementeer echte Salesforce en SAP connecties
- Database migrations zijn niet inbegrepen (gebruikt EnsureCreated)

## Support
Voor vragen of problemen, check de logs in de console output.

## Licentie
Demo applicatie voor educatieve doeleinden.
