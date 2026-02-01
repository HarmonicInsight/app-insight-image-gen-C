using InsightMediaGenerator.Services.Interfaces;

namespace InsightMediaGenerator.Api.Endpoints;

public static class ModelEndpoints
{
    public static void MapModelEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/models")
            .WithTags("Models");

        group.MapGet("/", async (IStableDiffusionService sdService) =>
        {
            var models = await sdService.GetModelsAsync();
            return ApiResponse<IReadOnlyList<string>>.Ok(models);
        })
        .WithName("GetModels")
        .WithDescription("List available Stable Diffusion checkpoint models");

        group.MapGet("/loras", async (IStableDiffusionService sdService) =>
        {
            var loras = await sdService.GetLorasAsync();
            return ApiResponse<IReadOnlyList<string>>.Ok(loras);
        })
        .WithName("GetLoras")
        .WithDescription("List available LoRA models");

        group.MapGet("/samplers", () =>
        {
            var samplers = new List<string>
            {
                "DPM++ 2M Karras",
                "DPM++ SDE Karras",
                "DPM++ 2M SDE Karras",
                "Euler a",
                "Euler",
                "Heun",
                "LMS",
                "DDIM"
            };
            return ApiResponse<List<string>>.Ok(samplers);
        })
        .WithName("GetSamplers")
        .WithDescription("List available sampler algorithms");

        group.MapGet("/resolutions", () =>
        {
            var resolutions = new List<object>
            {
                new { width = 512, height = 512, label = "512x512" },
                new { width = 768, height = 768, label = "768x768" },
                new { width = 1024, height = 1024, label = "1024x1024" },
                new { width = 512, height = 768, label = "512x768 (Portrait)" },
                new { width = 768, height = 512, label = "768x512 (Landscape)" }
            };
            return ApiResponse<List<object>>.Ok(resolutions);
        })
        .WithName("GetResolutions")
        .WithDescription("List recommended image resolutions");
    }
}
