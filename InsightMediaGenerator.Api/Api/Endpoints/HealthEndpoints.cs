using InsightMediaGenerator.Models;
using InsightMediaGenerator.Services.Interfaces;

namespace InsightMediaGenerator.Api.Endpoints;

public static class HealthEndpoints
{
    private static readonly DateTime StartTime = DateTime.UtcNow;

    public static void MapHealthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api")
            .WithTags("Health");

        group.MapGet("/health", () =>
        {
            var uptime = (long)(DateTime.UtcNow - StartTime).TotalSeconds;
            return ApiResponse<HealthResponse>.Ok(new HealthResponse
            {
                Status = "ok",
                Version = "2.0.0",
                UptimeSeconds = uptime
            });
        })
        .WithName("GetHealth")
        .WithDescription("Health check endpoint");

        group.MapGet("/status", async (
            IStableDiffusionService sdService,
            IVoicevoxService vvService,
            AppConfig config) =>
        {
            var sdTask = sdService.CheckConnectionAsync();
            var vvTask = vvService.CheckConnectionAsync();
            await Task.WhenAll(sdTask, vvTask);

            return ApiResponse<ConnectionStatus>.Ok(new ConnectionStatus
            {
                StableDiffusion = new ServiceStatus
                {
                    Connected = sdTask.Result,
                    Url = config.StableDiffusion.ApiUrl
                },
                Voicevox = new ServiceStatus
                {
                    Connected = vvTask.Result,
                    Url = config.Voicevox.ApiUrl
                }
            });
        })
        .WithName("GetStatus")
        .WithDescription("Check connection status of Stable Diffusion and VOICEVOX");

        group.MapGet("/config", (AppConfig config) =>
        {
            return ApiResponse<DefaultsConfig>.Ok(config.Defaults);
        })
        .WithName("GetConfig")
        .WithDescription("Get current default generation parameters");
    }
}
