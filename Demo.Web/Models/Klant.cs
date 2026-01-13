namespace Demo.Web.Models;

public class Klant
{
    public int Id { get; set; }
    public string Naam { get; set; } = string.Empty;
    
    // ?? Encrypted fields - stored as encrypted in database
    public string Email { get; set; } = string.Empty; // Encrypted PII
    public string Telefoon { get; set; } = string.Empty; // Encrypted PII
    public string Adres { get; set; } = string.Empty; // Encrypted PII
}
