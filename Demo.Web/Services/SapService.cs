using Demo.Web.Models;
using System.Text;
using System.Xml.Linq;

namespace Demo.Web.Services;

public interface ISapService
{
    Task<SapResponse> SendOrderToSapAsync(Order order);
}

public class SapResponse
{
    public bool Success { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string IDocNumber { get; set; } = string.Empty;
}

public class SapService : ISapService
{
    private readonly ILogger<SapService> _logger;
    private readonly IConfiguration _configuration;

    public SapService(ILogger<SapService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<SapResponse> SendOrderToSapAsync(Order order)
    {
        try
        {
            var idocXml = GenerateIdocXml(order);
            
            _logger.LogInformation($"SAP iDoc gegenereerd voor order: {order.OrderNummer}");
            _logger.LogDebug($"iDoc XML: {idocXml}");

            // Simulatie van SAP communicatie
            await Task.Delay(500);

            var success = Random.Shared.Next(0, 100) > 10;

            return new SapResponse
            {
                Success = success,
                Status = success ? "53" : "51",
                Message = success ? "iDoc succesvol verwerkt" : "iDoc verwerking mislukt",
                IDocNumber = $"IDOC{order.Id:D10}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Fout bij SAP iDoc verzending: {ex.Message}");
            return new SapResponse
            {
                Success = false,
                Status = "51",
                Message = $"Fout: {ex.Message}",
                IDocNumber = string.Empty
            };
        }
    }

    private string GenerateIdocXml(Order order)
    {
        var idoc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("ORDERS05",
                new XElement("IDOC",
                    new XAttribute("BEGIN", "1"),
                    new XElement("EDI_DC40",
                        new XAttribute("SEGMENT", "1"),
                        new XElement("IDOCTYP", "ORDERS05"),
                        new XElement("MESTYP", "ORDERS"),
                        new XElement("SNDPRN", "DEMOWEBAPP"),
                        new XElement("RCVPRN", "SAPR3")
                    ),
                    new XElement("E1EDK01",
                        new XAttribute("SEGMENT", "1"),
                        new XElement("BELNR", order.OrderNummer),
                        new XElement("DATUM", order.OrderDatum.ToString("yyyyMMdd")),
                        new XElement("WKURS", order.TotaalBedrag.ToString("F2"))
                    ),
                    new XElement("E1EDK02",
                        new XAttribute("SEGMENT", "1"),
                        new XElement("QUALF", "001"),
                        new XElement("BELNR", order.OrderNummer)
                    ),
                    new XElement("E1EDKA1",
                        new XAttribute("SEGMENT", "1"),
                        new XElement("PARVW", "AG"),
                        new XElement("PARTN", order.KlantId.ToString()),
                        new XElement("NAME1", order.Klant?.Naam ?? "")
                    ),
                    from regel in order.OrderRegels
                    select new XElement("E1EDP01",
                        new XAttribute("SEGMENT", "1"),
                        new XElement("POSEX", regel.Id.ToString()),
                        new XElement("MENGE", regel.Aantal.ToString()),
                        new XElement("MENEE", "PCE"),
                        new XElement("WERKS", "1000"),
                        new XElement("E1EDP19",
                            new XElement("QUALF", "002"),
                            new XElement("IDTNR", regel.Boek?.ISBN ?? ""),
                            new XElement("KTEXT", regel.Boek?.Titel ?? "")
                        ),
                        new XElement("E1EDP26",
                            new XElement("QUALF", "003"),
                            new XElement("BETRG", regel.Prijs.ToString("F2"))
                        )
                    )
                )
            )
        );

        return idoc.ToString();
    }
}
