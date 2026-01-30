using System.Text.Json;
using InsightMediaGenerator.Api;
using InsightMediaGenerator.Api.Endpoints;
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

// ── CORS (allow external tools to access the API) ──
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
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

// ── Middleware ──
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "InsightMovie API v1");
        options.RoutePrefix = string.Empty; // Swagger UI at root
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
    logger.LogInformation("===========================================");
    logger.LogInformation("  InsightMovie API Server v{Version}", config.App.Version);
    logger.LogInformation("  Swagger UI: http://localhost:5100");
    logger.LogInformation("  API Base:   http://localhost:5100/api");
    logger.LogInformation("===========================================");
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

            // Try parsing as InsightMovie config format first
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
