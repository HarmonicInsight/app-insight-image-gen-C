namespace InsightMediaGenerator.Api.Middleware;

public class RequestTimeoutMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TimeSpan _timeout;

    public RequestTimeoutMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        var seconds = configuration.GetValue<int>("ApiSecurity:RequestTimeoutSeconds", 300);
        _timeout = TimeSpan.FromSeconds(seconds);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
        cts.CancelAfter(_timeout);
        context.RequestAborted = cts.Token;

        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (!context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                error = $"Request timed out after {_timeout.TotalSeconds}s",
                timestamp = DateTime.UtcNow
            });
        }
    }
}

public static class RequestTimeoutExtensions
{
    public static IApplicationBuilder UseRequestTimeout(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestTimeoutMiddleware>();
    }
}
