namespace Demo.Web.Services;

public interface ISalesforceService
{
    Task<string> CreateOrderAsync(object orderData);
}

public class SalesforceService : ISalesforceService
{
    private readonly ILogger<SalesforceService> _logger;

    public SalesforceService(ILogger<SalesforceService> logger)
    {
        _logger = logger;
    }

    public async Task<string> CreateOrderAsync(object orderData)
    {
        await Task.Delay(300);
        
        var salesforceId = $"SF{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        _logger.LogInformation($"Order aangemaakt in Salesforce met ID: {salesforceId}");
        
        return salesforceId;
    }
}
