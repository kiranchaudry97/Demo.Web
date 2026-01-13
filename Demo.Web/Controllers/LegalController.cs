using Microsoft.AspNetCore.Mvc;

namespace Demo.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LegalController : ControllerBase
{
    [HttpGet("privacy-policy")]
    public ActionResult GetPrivacyPolicy()
    {
        var policy = new
        {
            lastUpdated = "2024-01-15",
            version = "1.0",
            language = "nl",
            sections = new[]
            {
                new
                {
                    title = "Wie zijn wij?",
                    content = @"Bookstore API is een systeem voor het beheren van boekverkoop orders. 
                    Wij verzamelen en verwerken persoonsgegevens uitsluitend voor het uitvoeren van bestellingen 
                    en klantenadministratie."
                },
                new
                {
                    title = "Welke gegevens verzamelen wij?",
                    content = @"Wij verzamelen de volgende persoonsgegevens:
                    - Naam (verplicht voor orderverwerking)
                    - E-mailadres (verplicht voor communicatie)
                    - Telefoonnummer (optioneel voor contact)
                    - Adres (verplicht voor levering)
                    - Ordergeschiedenis
                    
                    Wij verzamelen GEEN:
                    - Geboortedatum
                    - Burgerservicenummer (BSN)
                    - Financiële gegevens (creditcards)
                    - Gevoelige categorieën (gezondheid, religie, etc.)"
                },
                new
                {
                    title = "Waarom verzamelen wij uw gegevens?",
                    content = @"Rechtsgrondslag voor verwerking:
                    1. Uitvoering van overeenkomst (Art. 6.1.b GDPR) - Orders verwerken
                    2. Toestemming (Art. 6.1.a GDPR) - Via API call
                    3. Gerechtvaardigd belang (Art. 6.1.f GDPR) - Fraudepreventie
                    
                    Doeleinden:
                    - Orderverwerking
                    - Klantenadministratie
                    - Communicatie over uw bestelling
                    - Wettelijke verplichtingen (fiscaal)"
                },
                new
                {
                    title = "Hoe lang bewaren wij uw gegevens?",
                    content = @"Bewaartermijnen:
                    - Ordergegevens: 7 jaar (fiscale verplichting)
                    - Klantgegevens: Tot verwijderingsverzoek
                    - Backups: 30 dagen
                    - Logs: 7 dagen
                    
                    Na deze termijn worden gegevens permanent verwijderd."
                },
                new
                {
                    title = "Met wie delen wij uw gegevens?",
                    content = @"Ontvangers van uw gegevens:
                    1. Salesforce (CRM) - Klantbeheer
                    2. SAP R/3 (ERP) - Orderverwerking
                    3. RabbitMQ (Queue) - Interne message broker
                    
                    Wij geven uw gegevens NIET door aan:
                    - Marketingbedrijven
                    - Social media platforms
                    - Derde partijen zonder toestemming
                    
                    Doorgifte buiten EU: Nee (alle data binnen EU)"
                },
                new
                {
                    title = "Uw rechten onder de AVG/GDPR",
                    content = @"U heeft de volgende rechten:
                    
                    1. Recht op inzage (Art. 15)
                       GET /api/klanten/{id}
                    
                    2. Recht op correctie (Art. 16)
                       PUT /api/klanten/{id}
                    
                    3. Recht op verwijdering (Art. 17)
                       DELETE /api/klanten/{id}
                    
                    4. Recht op dataportabiliteit (Art. 20)
                       GET /api/klanten/{id}/export
                    
                    5. Recht op beperking verwerking (Art. 18)
                       Contact: privacy@bookstore.com
                    
                    6. Recht van bezwaar (Art. 21)
                       Contact: privacy@bookstore.com
                    
                    Verzoeken behandelen wij binnen 1 maand."
                },
                new
                {
                    title = "Beveiliging van uw gegevens",
                    content = @"Beveiligingsmaatregelen:
                    - API Key authenticatie (X-API-Key header)
                    - HTTPS encryptie (TLS 1.2+)
                    - Input validatie tegen SQL injection en XSS
                    - Audit logging van alle acties
                    - Dagelijkse backups (encrypted)
                    - Access control (alleen geautoriseerd personeel)
                    
                    Bij een datalek informeren wij u binnen 72 uur conform GDPR."
                },
                new
                {
                    title = "Cookies en tracking",
                    content = @"Deze API gebruikt:
                    - Geen cookies (API-only, geen webinterface sessies)
                    - Geen tracking pixels
                    - Geen third-party analytics
                    
                    Zie Cookie Policy voor meer details."
                },
                new
                {
                    title = "Contact & Klachten",
                    content = @"Vragen of klachten over privacy?
                    
                    E-mail: privacy@bookstore.com
                    Data Protection Officer: dpo@bookstore.com
                    
                    Klacht indienen bij toezichthouder:
                    Autoriteit Persoonsgegevens
                    Postbus 93374
                    2509 AJ Den Haag
                    Website: autoriteitpersoonsgegevens.nl"
                },
                new
                {
                    title = "Wijzigingen in deze policy",
                    content = @"Wij kunnen deze privacy policy aanpassen. 
                    De laatste versie vindt u altijd op deze pagina.
                    Belangrijke wijzigingen communiceren wij via e-mail.
                    
                    Check regelmatig deze pagina voor updates."
                }
            },
            contact = new
            {
                email = "privacy@bookstore@ehb.be",
                dpo = "dpo@bookstore@ehb.be",
                address = "Bookstore API, Nijverheidskaai 170, 1070 Brussel"
            }
        };

        return Ok(policy);
    }

    [HttpGet("cookie-policy")]
    public ActionResult GetCookiePolicy()
    {
        var policy = new
        {
            lastUpdated = "2024-01-15",
            version = "1.0",
            language = "nl",
            summary = "Deze API gebruikt geen cookies voor authenticatie of sessiemanagement.",
            sections = new[]
            {
                new
                {
                    title = "Wat zijn cookies?",
                    content = @"Cookies zijn kleine tekstbestanden die op uw apparaat worden opgeslagen 
                    wanneer u een website bezoekt. Ze worden gebruikt voor verschillende doeleinden, 
                    zoals het onthouden van voorkeuren of het volgen van gebruikersgedrag."
                },
                new
                {
                    title = "Gebruikt deze API cookies?",
                    content = @"NEEN - Deze API gebruikt GEEN cookies.
                    
                    Reden: Dit is een REST API die authenticatie doet via API Keys in headers (X-API-Key).
                    Er zijn geen sessies die onthouden moeten worden.
                    
                    De web interface (indien gebruikt) gebruikt mogelijk:
                    - LocalStorage voor client-side state management
                    - Geen cookies voor authenticatie
                    - Geen third-party tracking cookies"
                },
                new
                {
                    title = "Welke alternatieven gebruiken wij?",
                    content = @"In plaats van cookies:
                    
                    1. API Key Authenticatie
                       - X-API-Key: demo-api-key-12345
                       - Geen sessies op server
                       - Stateless authentication
                    
                    2. LocalStorage (client-side)
                       - Tijdelijke opslag in browser
                       - Geen data naar server
                       - Alleen voor UI state
                    
                    3. HTTP Headers
                       - Content-Type: application/json
                       - Authorization informatie in headers"
                },
                new
                {
                    title = "Third-party cookies",
                    content = @"Wij gebruiken GEEN third-party cookies:
                    - Geen Google Analytics
                    - Geen Facebook Pixel
                    - Geen advertentienetwerken
                    - Geen social media tracking
                    
                    Uw privacy is belangrijk voor ons."
                },
                new
                {
                    title = "Uw keuzes",
                    content = @"Omdat wij geen cookies gebruiken, hoeft u niets te accepteren of weigeren.
                    
                    Als u de web interface gebruikt:
                    - LocalStorage kan verwijderd worden via browser instellingen
                    - Geen impact op API functionaliteit
                    - Data wordt niet gedeeld met derden"
                },
                new
                {
                    title = "Wijzigingen in dit beleid",
                    content = @"Als wij in de toekomst cookies gaan gebruiken:
                    - Informeren wij u vooraf
                    - Vragen wij expliciete toestemming
                    - Updaten wij deze Cookie Policy
                    
                    Laatste update: 15 januari 2024"
                }
            },
            technicalDetails = new
            {
                apiAuthentication = "X-API-Key header (stateless)",
                sessionManagement = "None (stateless API)",
                clientStorage = "LocalStorage (optional, client-side only)",
                thirdPartyTracking = "None"
            }
        };

        return Ok(policy);
    }

    [HttpGet("terms-of-service")]
    public ActionResult GetTermsOfService()
    {
        var terms = new
        {
            lastUpdated = "2024-01-15",
            version = "1.0",
            language = "nl",
            sections = new[]
            {
                new
                {
                    title = "1. De Service",
                    content = "De Bookstore API biedt order management, klantenbeheer en voorraadmanagement voor boekverkoop."
                },
                new
                {
                    title = "2. API Key",
                    content = @"- U ontvangt een unieke API Key (X-API-Key header)
                    - Houd uw API Key geheim
                    - Maximum 100 requests per minuut
                    - Bij misbruik kunnen we uw toegang blokkeren"
                },
                new
                {
                    title = "3. Toegestaan Gebruik",
                    content = @"Toegestaan: Orders plaatsen, klanten- en voorraadmanagement, integratie met eigen systemen.
                    
                    Verboden: Misbruik, hacking, reverse engineering, doorverkoop van toegang, spam of ongeautoriseerde marketing."
                },
                new
                {
                    title = "4. Uw Data",
                    content = @"- U behoudt eigendom van uw klantgegevens
                    - Wij voldoen aan AVG/GDPR
                    - U kunt uw data altijd exporteren
                    - Zie Privacy Policy voor details"
                },
                new
                {
                    title = "5. Beschikbaarheid",
                    content = @"- Wij streven naar 99.5% uptime
                    - Onderhoud wordt vooraf aangekondigd
                    - Service wordt geleverd 'as is'
                    - Support: support@bookstore.com"
                },
                new
                {
                    title = "6. Aansprakelijkheid",
                    content = @"- Maximale aansprakelijkheid beperkt tot abonnementsgeld
                    - Geen aansprakelijkheid voor indirecte schade
                    - Maak altijd backups van uw data"
                },
                new
                {
                    title = "7. Wijzigingen & Opzegging",
                    content = @"- Wij kunnen deze voorwaarden wijzigen (30 dagen vooraf melding)
                    - U kunt opzeggen met 1 maand opzegtermijn
                    - Wij kunnen uw toegang beëindigen bij misbruik"
                },
                new
                {
                    title = "8. Toepasselijk Recht",
                    content = "Nederlands recht is van toepassing. Geschillen: Rechtbank Amsterdam."
                }
            },
            contact = new
            {
                email = "legal@bookstore@ehb.be",
                phone = "+32 2 523 37 37",
                address = "Bookstore API BV, Nijverheidskaai 170, 1070 Brussel",
                kvk = "0123.456.789",
                btw = "BE0123456789"
            },
            acceptance = new
            {
                method = "Gebruik van de API impliceert acceptatie van deze voorwaarden"
            }
        };

        return Ok(terms);
    }

    [HttpGet("gdpr-info")]
    public ActionResult GetGdprInfo()
    {
        var info = new
        {
            complianceLevel = "Substantially Compliant",
            lastAudit = "2024-01-15",
            rights = new[]
            {
                new { right = "Inzage", endpoint = "GET /api/klanten/{id}", implemented = true },
                new { right = "Correctie", endpoint = "PUT /api/klanten/{id}", implemented = true },
                new { right = "Verwijdering", endpoint = "DELETE /api/klanten/{id}", implemented = true },
                new { right = "Portabiliteit", endpoint = "GET /api/klanten/{id}/export", implemented = true },
                new { right = "Beperking", endpoint = "Contact support", implemented = false },
                new { right = "Bezwaar", endpoint = "Contact support", implemented = false }
            },
            dataProtectionOfficer = new
            {
                name = "DPO Bookstore API",
                email = "dpo@bookstore@ehb.be",
                phone = "+32 2 523 37 37"
            },
            supervisoryAuthority = new
            {
                name = "Autoriteit Persoonsgegevens",
                website = "https://autoriteitpersoonsgegevens.nl",
                email = "info@autoriteitpersoonsgegevens.nl",
                phone = "+31 70 888 8500"
            },
            certifications = new[]
            {
                "GDPR Compliant (self-assessment)",
                "ISO 27001 (planned)",
                "SOC 2 Type II (planned)"
            }
        };

        return Ok(info);
    }
}
