namespace Demo.Web.Middleware;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private static readonly string[] ApiKeyHeaderNames = ["X-API-Key", "X-Api-Key", "X-APIKEY", "X-ApiKey", "ApiKey", "Api-Key"];

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip API key check voor deze paths
        if (context.Request.Path.StartsWithSegments("/swagger") || 
            context.Request.Path.StartsWithSegments("/openapi") ||
            context.Request.Path.Value == "/" ||
            context.Request.Path.StartsWithSegments("/index.html") ||
            context.Request.Path.StartsWithSegments("/app.js") ||
            !context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        string? extractedApiKey = null;
        foreach (var headerName in ApiKeyHeaderNames)
        {
            if (context.Request.Headers.TryGetValue(headerName, out var headerValue) && !string.IsNullOrWhiteSpace(headerValue))
            {
                extractedApiKey = headerValue.ToString();
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(extractedApiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API Key ontbreekt" });
            return;
        }

        var apiKey = _configuration["ApiKey"] ?? "demo-api-key-12345";

        if (!apiKey.Equals(extractedApiKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Ongeldige API Key" });
            return;
        }

        await _next(context);
    }
}
