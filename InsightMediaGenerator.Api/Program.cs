using System.Text.Json;
using System.Threading.RateLimiting;
using InsightMediaGenerator.Api;
using InsightMediaGenerator.Api.Endpoints;
using InsightMediaGenerator.Api.Middleware;
using InsightMediaGenerator.Models;
using InsightMediaGenerator.Services;
using InsightMediaGenerator.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ── Load AppConfig ──
var config = LoadConfiguration();
builder.Services.AddSingleton(config);

// ── Register core services (same as WPF app) ──
builder.Services.AddSingleton<IFileService, FileService>();
builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
builder.Services.AddHttpClient<IStableDiffusionService, StableDiffusionService>();
builder.Services.AddHttpClient<IVoicevoxService, VoicevoxService>();

// ── Register API-specific services ──
builder.Services.AddSingleton<JobService>();

// ── Swagger / OpenAPI ──
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "InsightMovie API",
        Version = "v1",
        Description = """
            InsightMovie REST API for external automation.

            Enables AI agents (Claude Code, etc.) and programs (Python, etc.)
            to automate image and audio generation workflows.

            ## Authentication
            Set `X-API-Key` header with your API key (configured in appsettings.json).
            If no key is configured, authentication is disabled (development mode).

            ## Features
            - Image generation (single & batch) via Stable Diffusion
            - Audio synthesis via VOICEVOX
            - Async job management with progress tracking
            - Multi-step pipeline execution
            - Character prompt file management

            ## Async Pattern
            For long-running operations, use the `/async` variants.
            They return a job ID immediately. Poll `GET /api/jobs/{job_id}` for status.
            """
    });
});

// ── CORS ──
var allowedOrigins = builder.Configuration.GetSection("ApiSecurity:AllowedOrigins").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins != null && allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            // Development: allow all (secured by API key)
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

// ── Rate Limiting ──
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Global: 30 requests/minute per IP
    options.AddPolicy("global", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1)
            }));

    // Generation: 5 concurrent generation requests per IP
    options.AddPolicy("generation", context =>
        RateLimitPartition.GetConcurrencyLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new ConcurrencyLimiterOptions
            {
                PermitLimit = 5,
                QueueLimit = 10
            }));
});

// ── Request size limit ──
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
});

var app = builder.Build();

// ── Initialize database ──
var dbService = app.Services.GetRequiredService<IDatabaseService>();
await dbService.InitializeAsync();

// ── Auto-discover VOICEVOX if configured ──
if (config.Voicevox.AutoDiscover)
{
    var vvService = app.Services.GetRequiredService<IVoicevoxService>();
    await vvService.DiscoverEngineAsync();
}

// ── Middleware pipeline (order matters) ──
app.UseCors();
app.UseApiKeyAuth();
app.UseRequestTimeout();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "InsightMovie API v1");
        options.RoutePrefix = string.Empty;
    });
}

// ── Map all endpoint groups ──
app.MapHealthEndpoints();
app.MapModelEndpoints();
app.MapImageEndpoints();
app.MapAudioEndpoints();
app.MapPromptEndpoints();
app.MapJobEndpoints();
app.MapPipelineEndpoints();

// ── Startup banner ──
app.Lifetime.ApplicationStarted.Register(() =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var apiKey = builder.Configuration.GetValue<string>("ApiSecurity:ApiKey");
    var authStatus = string.IsNullOrEmpty(apiKey) ? "DISABLED (dev)" : "ENABLED";

    logger.LogInformation("===========================================");
    logger.LogInformation("  InsightMovie API Server v{Version}", config.App.Version);
    logger.LogInformation("  Swagger UI: http://localhost:5100");
    logger.LogInformation("  API Base:   http://localhost:5100/api");
    logger.LogInformation("  Auth:       {AuthStatus}", authStatus);
    logger.LogInformation("===========================================");
});

// ── Graceful shutdown: dispose job service ──
app.Lifetime.ApplicationStopping.Register(() =>
{
    var jobService = app.Services.GetRequiredService<JobService>();
    jobService.Dispose();
});

app.Run();

// ── Configuration loader (matches WPF app format) ──
static AppConfig LoadConfiguration()
{
    var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

    if (File.Exists(configPath))
    {
        try
        {
            var json = File.ReadAllText(configPath);
            var appConfig = JsonSerializer.Deserialize<AppConfig>(json);
            if (appConfig != null && !string.IsNullOrEmpty(appConfig.App.Name))
            {
                return appConfig;
            }
        }
        catch
        {
            // Fall through to default
        }
    }

    return new AppConfig();
}
