namespace Demo.Web.Models;

public class OrderMessage
{
    public string OrderNummer { get; set; } = string.Empty;
    public string KlantNaam { get; set; } = string.Empty;
    public string KlantEmail { get; set; } = string.Empty;
    public DateTime OrderDatum { get; set; }
    public decimal TotaalBedrag { get; set; }
    public List<OrderItemMessage> Items { get; set; } = new();
}

public class OrderItemMessage
{
    public string BoekTitel { get; set; } = string.Empty;
    public int Aantal { get; set; }
    public decimal Prijs { get; set; }
}
