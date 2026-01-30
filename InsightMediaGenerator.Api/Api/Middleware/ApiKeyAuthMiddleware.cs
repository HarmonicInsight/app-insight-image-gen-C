namespace InsightMediaGenerator.Api.Middleware;

public class ApiKeyAuthMiddleware
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private readonly RequestDelegate _next;
    private readonly string? _apiKey;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;

    public ApiKeyAuthMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyAuthMiddleware> logger)
    {
        _next = next;
        _apiKey = configuration.GetValue<string>("ApiSecurity:ApiKey");
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip auth for health check and Swagger
        var path = context.Request.Path.Value ?? "";
        if (path == "/api/health"
            || path.StartsWith("/swagger")
            || path == "/"
            || path.StartsWith("/index.html"))
        {
            await _next(context);
            return;
        }

        // If no API key is configured, auth is disabled (development mode)
        if (string.IsNullOrEmpty(_apiKey))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedKey)
            || providedKey.ToString() != _apiKey)
        {
            _logger.LogWarning("Unauthorized API request from {RemoteIp} to {Path}",
                context.Connection.RemoteIpAddress, path);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                error = "Invalid or missing API key. Set 'X-API-Key' header.",
                timestamp = DateTime.UtcNow
            });
            return;
        }

        await _next(context);
    }
}

public static class ApiKeyAuthExtensions
{
    public static IApplicationBuilder UseApiKeyAuth(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiKeyAuthMiddleware>();
    }
}
