# ?? Gebruikershandleiding - Bookstore Beheerpagina

## ?? Snel Starten

### 1. Start de applicatie
```bash
cd Demo.Web
dotnet run
```

### 2. Open de browser
Navigeer naar: **http://localhost:5269** of **https://localhost:7200**

---

## ?? Functionaliteiten Overzicht

### ? Wat kun je doen?

| Sectie | Functie | Beschrijving |
|--------|---------|--------------|
| **Bestellingen** | Bekijken | Alle orders met details, Salesforce ID en SAP status |
| **Klantenbeheer** | CRUD | Toevoegen, bewerken, verwijderen van klanten |
| **Klant Details** | Formulier | Klantgegevens invoeren en aanpassen |
| **Boeken** | CRUD | Beheer 10 boeken met voorraad en prijzen |
| **Bestel een boek** | Winkelmandje | Meerdere boeken bestellen in één order |

---

## ?? Een Bestelling Plaatsen (Stap voor Stap)

### Scenario: Verkoper plaatst order voor een klant

#### Stap 1: Klant Selecteren
- Scroll naar sectie **"Bestel een boek"**
- Klik op dropdown **"Klant"**
- Selecteer bijvoorbeeld: **Jan Jansen**

#### Stap 2: Boek Kiezen
- Klik op dropdown **"Boek"**
- Selecteer bijvoorbeeld: **C# in Depth - € 49.99 (Voorraad: 25)**
- Het systeem toont direct de prijs en beschikbare voorraad

#### Stap 3: Aantal Invoeren
- Vul het gewenste aantal in (bijvoorbeeld: **2**)
- Let op: Het systeem controleert automatisch of er voldoende voorraad is

#### Stap 4: Toevoegen aan Winkelmandje
- Klik op **"Toevoegen aan winkelmandje"**
- Het boek verschijnt in het winkelmandje met subtotaal

#### Stap 5: Meer Boeken Toevoegen (Optioneel)
- Herhaal stap 2-4 voor andere boeken
- Bijvoorbeeld: Voeg **Clean Code (1x)** toe
- Alle items worden getoond in het winkelmandje

#### Stap 6: Controleer Winkelmandje
Het winkelmandje toont:
```
C# in Depth
€ 49.99 × 2 = € 99.98

Clean Code
€ 39.99 × 1 = € 39.99

Totaal: € 139.97
```

#### Stap 7: Order Plaatsen
- Klik op de groene knop **"Bestellen"**
- Het systeem:
  - ? Valideert de order
  - ? Controleert voorraad
  - ? Stuurt naar RabbitMQ (? Salesforce)
  - ? Genereert SAP iDoc (? SAP R/3)
  - ? Update voorraad automatisch

#### Stap 8: Bevestiging
Je ziet een succesbericht met:
- **Order nummer**: ORD20240115123045
- **Totaal bedrag**: € 139.97
- **Salesforce ID**: SF12345678
- **SAP Status**: Status 53: iDoc succesvol verwerkt

---

## ?? Klanten Beheren

### Nieuwe Klant Toevoegen

**Optie 1: Via Klant Details Formulier**
1. Scroll naar **"Klant Details"** sectie
2. Vul de velden in:
   - Naam: **Maria de Vries**
   - Email: **maria@example.com**
   - Telefoon: **0687654321**
   - Adres: **Kerkstraat 10, Rotterdam**
3. Klik op **"Opslaan"**

**Optie 2: Via Modal**
1. Scroll naar **"Klantenbeheer"**
2. Klik op groene knop **"Nieuwe Klant"**
3. Vul het formulier in
4. Klik op **"Opslaan"**

### Klant Bewerken
1. Ga naar **"Klantenbeheer"** tabel
2. Zoek de klant (bijvoorbeeld: **Jan Jansen**)
3. Klik op oranje knop **"Bewerken"**
4. Pas de gegevens aan in de modal
5. Klik op **"Opslaan"**

### Klant Verwijderen
1. Ga naar **"Klantenbeheer"** tabel
2. Klik op rode knop **"Verwijderen"**
3. Bevestig de actie

?? **Let op**: Je kunt geen klanten verwijderen die orders hebben geplaatst.

---

## ?? Boeken Beheren

### De 10 Boeken in de Database

| # | Titel | Auteur | Prijs | Voorraad |
|---|-------|--------|-------|----------|
| 1 | C# in Depth | Jon Skeet | € 49.99 | 25 |
| 2 | Clean Code | Robert C. Martin | € 39.99 | 30 |
| 3 | The Pragmatic Programmer | Andrew Hunt | € 44.99 | 20 |
| 4 | Design Patterns | Gang of Four | € 54.99 | 15 |
| 5 | Refactoring | Martin Fowler | € 42.99 | 18 |
| 6 | Head First Design Patterns | Eric Freeman | € 37.99 | 22 |
| 7 | Code Complete | Steve McConnell | € 52.99 | 12 ?? |
| 8 | The Clean Coder | Robert C. Martin | € 34.99 | 28 |
| 9 | Working Effectively... | Michael Feathers | € 46.99 | 16 |
| 10 | Domain-Driven Design | Eric Evans | € 58.99 | 10 ?? |

?? = Lage voorraad (< 15) - wordt in rood getoond

### Nieuw Boek Toevoegen
1. Scroll naar **"Boeken"** sectie
2. Klik op groene knop **"Nieuw Boek"**
3. Vul het formulier in:
   - Titel: **Nieuwe Titel**
   - Auteur: **Auteur Naam**
   - Prijs: **29.99**
   - Voorraad: **50**
   - ISBN: **978-1234567890**
4. Klik op **"Opslaan"**

### Boek Voorraad Aanpassen
1. Ga naar **"Boeken"** tabel
2. Klik op **"Bewerken"** bij het gewenste boek
3. Pas het **"Voorraad Aantal"** veld aan
4. Klik op **"Opslaan"**

**Automatische Voorraad Update:**
- Bij elke bestelling wordt de voorraad **automatisch** verlaagd
- Voorbeeld: 2x "C# in Depth" besteld ? Voorraad: 25 ? 23

---

## ?? Bestellingen Bekijken

### Order Details Opvragen
1. Scroll naar **"Bestellingen"** sectie
2. Klik op **"Details"** knop bij een order
3. De modal toont:
   - Order nummer en datum
   - Klantnaam, email, telefoon
   - **Salesforce ID** (bijvoorbeeld: SF12345678)
   - **SAP Status** (bijvoorbeeld: Status 53: succesvol verwerkt)
   - Lijst met bestelde items
   - Aantal en prijzen per item
   - Totaalbedrag

---

## ?? Integratie Flow (Achter de Schermen)

Wat gebeurt er bij het plaatsen van een order?

```
1. Verkoper plaatst order via web interface
   ?
2. API ontvangt order + valideert met API Key
   ?
3. Controles:
   - Bestaat de klant?
   - Bestaan alle boeken?
   - Is er voldoende voorraad?
   ?
4. Order aanmaken in SQLite database
   ?
5. PARALLEL VERWERKING:
   ?? A) RabbitMQ ? Salesforce
   ?  - Publiceer naar queue "salesforce_orders"
   ?  - Salesforce Consumer maakt order aan
   ?  - Retourneert Salesforce ID
   ?
   ?? B) SAP iDoc ? SAP R/3
      - Transformeer JSON naar XML (ORDERS05)
      - Segment E1EDK01 met ordernummer
      - Status 64: Klaar voor verwerking
      - Status 53: Succesvol verwerkt ?
      - Status 51: Fout opgetreden ?
   ?
6. Response met beide resultaten
   ?
7. Voorraad automatisch bijgewerkt
   ?
8. Bevestiging getoond aan gebruiker
```

---

## ?? Technische Details

### API Key
Alle API calls gebruiken automatisch de API Key: **demo-api-key-12345**

Dit gebeurt transparent in de `app.js` file:
```javascript
const headers = {
    'Content-Type': 'application/json',
    'X-API-Key': 'demo-api-key-12345'
};
```

### Database
- **Type**: SQLite
- **Locatie**: `Demo.Web/bookstore.db`
- **Initiële data**: 3 klanten + 10 boeken

### RabbitMQ (Optioneel)
Als RabbitMQ niet draait, werkt de app in **fallback mode**:
- Orders worden wel verwerkt
- Salesforce integratie wordt gesimuleerd
- SAP iDoc wordt wel gegenereerd

---

## ?? Veelvoorkomende Scenario's

### Scenario 1: Lage Voorraad Waarschuwing
**Probleem**: Boek heeft lage voorraad  
**Oplossing**: 
1. Ga naar "Boeken" sectie
2. Boeken met < 15 voorraad zijn rood gemarkeerd
3. Bewerk het boek en verhoog de voorraad

### Scenario 2: Onvoldoende Voorraad bij Bestellen
**Probleem**: "Onvoldoende voorraad voor 'Titel'"  
**Oplossing**:
1. Check huidige voorraad in dropdown (toont automatisch)
2. Pas aantal aan naar beschikbare voorraad
3. Of verhoog eerst de voorraad via "Bewerken"

### Scenario 3: Order Niet Geslaagd
**Probleem**: Error bij order plaatsing  
**Check**:
- Is een klant geselecteerd? ?
- Zijn er items in winkelmandje? ?
- Is er voldoende voorraad? ?

### Scenario 4: SAP Status 51 (Fout)
**Betekenis**: SAP kon de order niet verwerken  
**Note**: Dit is een simulatie - in productie contact SAP team

---

## ?? Swagger API Documentatie

Voor ontwikkelaars en testing:

**URL**: https://localhost:7200/swagger

Hier vind je:
- Alle API endpoints
- Request/Response voorbeelden
- "Try it out" functie voor directe testing
- Schema's en modellen

**Let op**: Voeg API Key toe via "Authorize" knop: `demo-api-key-12345`

---

## ?? Support & Vragen

### Logs Bekijken
Alle acties worden gelogd in de console:
- Order creatie
- RabbitMQ publicatie
- SAP iDoc generatie
- Salesforce integratie
- CRUD operaties

### Database Resetten
```bash
# Stop de applicatie
# Verwijder de database file
rm Demo.Web/bookstore.db

# Start de applicatie opnieuw
dotnet run

# Database wordt opnieuw aangemaakt met originele data
```

---

## ? Tips & Tricks

1. **Voorraad in de Gaten Houden**
   - Rode waarschuwing = < 15 items
   - Voorraad wordt realtime bijgewerkt na orders

2. **Meerdere Items Bestellen**
   - Gebruik het winkelmandje voor efficiëntie
   - Voeg alle boeken toe voordat je bestelt

3. **Klant Details Formulier**
   - Gebruik voor snelle klant toevoegingen
   - Modal is handig voor bewerken bestaande klanten

4. **Order Geschiedenis**
   - Alle orders blijven bewaard
   - Details tonen volledige transactie info

5. **Testen met Swagger**
   - Ideaal voor API testing
   - Dezelfde functionaliteit als web interface

---

**Veel succes met de Bookstore Beheerpagina! ???**
