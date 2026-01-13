# ?? START HIER - Bookstore Applicatie

## Snelstart in 3 stappen

### Stap 1: Open Terminal
Open een **PowerShell** of **Command Prompt** in deze folder

### Stap 2: Start de Applicatie
```bash
dotnet run
```

### Stap 3: Open Browser
Wacht tot je dit ziet:
```
Now listening on: http://localhost:5269
Now listening on: https://localhost:7200
```

Dan open je **één van deze URLs** in je browser:

- **HTTP**: http://localhost:5269
- **HTTPS**: https://localhost:7200

---

## ? Wat je zou moeten zien

Een webpagina met de titel **"Beheerpagina"** en deze secties:
1. Bestellingen (tabel)
2. Klantenbeheer (tabel)
3. Klant Details (formulier)
4. Boeken (tabel met 10 boeken)
5. Bestel een boek (formulier met winkelmandje)

---

## ? Problemen?

### Fout: "address already in use"
**Oplossing:**
```bash
# Stop alle dotnet processen
taskkill /F /IM dotnet.exe

# Start opnieuw
dotnet run
```

### Browser toont niets / witte pagina
**Controleer:**
1. Is de applicatie gestart? (zie console output)
2. Gebruik je de juiste URL? (http://localhost:5269)
3. Refresh de pagina (F5 of Ctrl+R)

### Browser toont "Connection refused"
**Oplossing:**
- De applicatie draait niet
- Start opnieuw met `dotnet run`
- Wacht tot je "Now listening on..." ziet

### Zie je alleen JSON tekst?
**Controleer URL:**
- ? Fout: http://localhost:5269/api/klanten (dit is de API)
- ? Goed: http://localhost:5269 (dit is de webpagina)

---

## ?? Test de Applicatie

### Quick Test - Een Order Plaatsen:
1. Scroll naar beneden naar **"Bestel een boek"**
2. Selecteer **"Klant"**: Jan Jansen
3. Selecteer **"Boek"**: C# in Depth
4. Vul **"Aantal"**: 2
5. Klik **"Toevoegen aan winkelmandje"**
6. Klik **"Bestellen"**
7. Zie de bevestiging met Salesforce ID en SAP status! ?

---

## ?? Meer Informatie

- **Volledige gebruikershandleiding**: Zie `USAGE_GUIDE.md`
- **Technische documentatie**: Zie `README.md`
- **API Documentatie**: Open http://localhost:5269/swagger

---

## ?? API Key (voor developers)

De web interface gebruikt automatisch de API key.

Als je de API direct wilt testen (via Postman, curl, etc.):
```
X-API-Key: demo-api-key-12345
```

---

**Je bent er klaar voor! Start de applicatie en open je browser. ??**
