namespace Demo.Web.Models;

public class Klant
{
    public int Id { get; set; }
    public string Naam { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefoon { get; set; } = string.Empty;
    public string Adres { get; set; } = string.Empty;
}
