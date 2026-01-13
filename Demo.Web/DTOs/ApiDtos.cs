namespace Demo.Web.DTOs;

public class OrderCreateDto
{
    public int KlantId { get; set; }
    public List<OrderRegelDto> Items { get; set; } = new();
}

public class OrderRegelDto
{
    public int BoekId { get; set; }
    public int Aantal { get; set; }
}

public class OrderResponseDto
{
    public int OrderId { get; set; }
    public string OrderNummer { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? SalesforceId { get; set; }
    public string? SapStatus { get; set; }
    public decimal TotaalBedrag { get; set; }
    public DateTime OrderDatum { get; set; }
    public string Bericht { get; set; } = string.Empty;
}

public class KlantDto
{
    public int Id { get; set; }
    public string Naam { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefoon { get; set; } = string.Empty;
    public string Adres { get; set; } = string.Empty;
}

public class BoekDto
{
    public int Id { get; set; }
    public string Titel { get; set; } = string.Empty;
    public string Auteur { get; set; } = string.Empty;
    public decimal Prijs { get; set; }
    public int VoorraadAantal { get; set; }
    public string ISBN { get; set; } = string.Empty;
}
