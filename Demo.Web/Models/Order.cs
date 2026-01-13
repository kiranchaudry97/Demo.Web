namespace Demo.Web.Models;

public class Order
{
    public int Id { get; set; }
    public int KlantId { get; set; }
    public Klant? Klant { get; set; }
    public DateTime OrderDatum { get; set; }
    public string OrderNummer { get; set; } = string.Empty;
    public string Status { get; set; } = "Nieuw";
    public string? SalesforceId { get; set; }
    public string? SapStatus { get; set; }
    public List<OrderRegel> OrderRegels { get; set; } = new();
    public decimal TotaalBedrag { get; set; }
}

public class OrderRegel
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int BoekId { get; set; }
    public Boek? Boek { get; set; }
    public int Aantal { get; set; }
    public decimal Prijs { get; set; }
}
