namespace Demo.Web.Models;

public enum EntityType
{
    Klant,
    Boek,
    Order
}

public enum ActionType
{
    Created,
    Updated,
    Deleted
}

public class EntityChangeMessage
{
    public EntityType EntityType { get; set; }
    public ActionType Action { get; set; }
    public int EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string PerformedBy { get; set; } = "System";
    public Dictionary<string, object>? Data { get; set; }
}

public class KlantDeletedMessage
{
    public int KlantId { get; set; }
    public string KlantNaam { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; }
    public string Reason { get; set; } = "User requested deletion";
}

public class BoekDeletedMessage
{
    public int BoekId { get; set; }
    public string Titel { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public int LaatsteVoorraad { get; set; }
    public DateTime DeletedAt { get; set; }
    public string Reason { get; set; } = "User requested deletion";
}
