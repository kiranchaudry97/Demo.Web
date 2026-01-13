using Demo.Web.Models;

namespace Demo.Web.Services;

/// <summary>
/// Salesforce API Data Transfer Objects
/// Maps internal data structure to Salesforce schema
/// </summary>
public class SalesforceOrderDto
{
    public string OrderNumber__c { get; set; } = string.Empty;
    public string AccountName__c { get; set; } = string.Empty;
    public string CustomerEmail__c { get; set; } = string.Empty;
    public string OrderDate__c { get; set; } = string.Empty; // ISO 8601 format
    public decimal TotalAmount__c { get; set; }
    public string Status__c { get; set; } = "New";
    public string Source__c { get; set; } = "Web API";
    public List<SalesforceOrderLineDto> OrderLines__r { get; set; } = new();
}

public class SalesforceOrderLineDto
{
    public string ProductName__c { get; set; } = string.Empty;
    public int Quantity__c { get; set; }
    public decimal UnitPrice__c { get; set; }
    public decimal LineTotal__c { get; set; }
}

public interface ISalesforceMapper
{
    SalesforceOrderDto MapToSalesforce(OrderMessage orderMessage);
    SalesforceOrderDto MapToSalesforce(Order order);
}

public class SalesforceMapper : ISalesforceMapper
{
    private readonly ILogger<SalesforceMapper> _logger;

    public SalesforceMapper(ILogger<SalesforceMapper> logger)
    {
        _logger = logger;
    }

    public SalesforceOrderDto MapToSalesforce(OrderMessage orderMessage)
    {
        _logger.LogInformation($"??? Mapping OrderMessage naar Salesforce DTO: {orderMessage.OrderNummer}");

        var salesforceDto = new SalesforceOrderDto
        {
            OrderNumber__c = orderMessage.OrderNummer,
            AccountName__c = SanitizeAccountName(orderMessage.KlantNaam),
            CustomerEmail__c = ValidateEmail(orderMessage.KlantEmail),
            OrderDate__c = orderMessage.OrderDatum.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            TotalAmount__c = Math.Round(orderMessage.TotaalBedrag, 2),
            Status__c = "New",
            Source__c = "Demo.Web API",
            OrderLines__r = orderMessage.Items.Select(item => new SalesforceOrderLineDto
            {
                ProductName__c = SanitizeProductName(item.BoekTitel),
                Quantity__c = item.Aantal,
                UnitPrice__c = Math.Round(item.Prijs, 2),
                LineTotal__c = Math.Round(item.Aantal * item.Prijs, 2)
            }).ToList()
        };

        _logger.LogInformation($"? Mapping compleet: {salesforceDto.OrderLines__r.Count} orderregels");
        return salesforceDto;
    }

    public SalesforceOrderDto MapToSalesforce(Order order)
    {
        _logger.LogInformation($"??? Mapping Order entity naar Salesforce DTO: {order.OrderNummer}");

        var salesforceDto = new SalesforceOrderDto
        {
            OrderNumber__c = order.OrderNummer,
            AccountName__c = SanitizeAccountName(order.Klant?.Naam ?? "Unknown"),
            CustomerEmail__c = ValidateEmail(order.Klant?.Email ?? ""),
            OrderDate__c = order.OrderDatum.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            TotalAmount__c = Math.Round(order.TotaalBedrag, 2),
            Status__c = MapOrderStatus(order.Status),
            Source__c = "Demo.Web API",
            OrderLines__r = order.OrderRegels.Select(regel => new SalesforceOrderLineDto
            {
                ProductName__c = SanitizeProductName(regel.Boek?.Titel ?? "Unknown"),
                Quantity__c = regel.Aantal,
                UnitPrice__c = Math.Round(regel.Prijs, 2),
                LineTotal__c = Math.Round(regel.Aantal * regel.Prijs, 2)
            }).ToList()
        };

        return salesforceDto;
    }

    private string SanitizeAccountName(string name)
    {
        // Remove special characters that Salesforce doesn't accept
        var sanitized = name.Trim();
        if (sanitized.Length > 80) // Salesforce limit
            sanitized = sanitized.Substring(0, 80);
        return sanitized;
    }

    private string SanitizeProductName(string productName)
    {
        var sanitized = productName.Trim();
        if (sanitized.Length > 120) // Salesforce limit
            sanitized = sanitized.Substring(0, 120);
        return sanitized;
    }

    private string ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            _logger.LogWarning($"?? Ongeldig email adres: {email}");
            return "noreply@demo.com"; // Fallback
        }
        return email.Trim().ToLower();
    }

    private string MapOrderStatus(string internalStatus)
    {
        // Map internal status to Salesforce status values
        return internalStatus switch
        {
            "In behandeling" => "New",
            "Verzonden" => "Shipped",
            "Afgerond" => "Completed",
            "Geannuleerd" => "Cancelled",
            _ => "New"
        };
    }
}
