namespace Demo.Web.Models;

public class Boek
{
    public int Id { get; set; }
    public string Titel { get; set; } = string.Empty;
    public string Auteur { get; set; } = string.Empty;
    public decimal Prijs { get; set; }
    public int VoorraadAantal { get; set; }
    public string ISBN { get; set; } = string.Empty;
}
