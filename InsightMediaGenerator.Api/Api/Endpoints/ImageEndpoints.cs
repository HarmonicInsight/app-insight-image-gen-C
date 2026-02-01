using InsightMediaGenerator.Models;
using InsightMediaGenerator.Services.Interfaces;

namespace InsightMediaGenerator.Api.Endpoints;

public static class ImageEndpoints
{
    public static void MapImageEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/images")
            .WithTags("Images");

        // ── Synchronous single image generation ──
        group.MapPost("/generate", async (
            ImageGenerateApiRequest request,
            IStableDiffusionService sdService,
            IDatabaseService dbService,
            AppConfig config,
            CancellationToken ct) =>
        {
            var error = Validation.ValidateImageRequest(request);
            if (error != null)
                return Results.BadRequest(ApiResponse.Fail(error));

            var genRequest = new ImageGenerationRequest
            {
                Prompt = request.Prompt,
                NegativePrompt = request.NegativePrompt,
                Model = request.Model ?? config.Defaults.Model,
                Lora = request.Lora,
                LoraWeight = request.LoraWeight ?? config.Defaults.LoraWeight,
                Steps = request.Steps ?? config.Defaults.Steps,
                CfgScale = request.CfgScale ?? config.Defaults.CfgScale,
                Width = request.Width ?? config.Defaults.Width,
                Height = request.Height ?? config.Defaults.Height,
                SamplerName = request.Sampler ?? config.Defaults.Sampler,
                CharName = request.CharName,
                BatchCount = request.BatchCount
            };

            var result = await sdService.GenerateAsync(genRequest, ct);

            if (!result.Success)
                return Results.Json(
                    ApiResponse<List<ImageInfoResponse>>.Fail(result.ErrorMessage ?? "Generation failed"),
                    statusCode: StatusCodes.Status502BadGateway);

            foreach (var img in result.GeneratedImages)
                await dbService.SaveImageMetadataAsync(img);

            var response = result.GeneratedImages.Select(JobService.MapImageToResponse).ToList();
            return Results.Json(ApiResponse<List<ImageInfoResponse>>.Ok(response));
        })
        .WithName("GenerateImage")
        .WithDescription("Generate image(s) synchronously. Blocks until complete. Use /generate/async for long operations.");

        // ── Async single image generation (returns job ID) ──
        group.MapPost("/generate/async", (
            ImageGenerateApiRequest request,
            JobService jobService) =>
        {
            var error = Validation.ValidateImageRequest(request);
            if (error != null)
                return Results.BadRequest(ApiResponse.Fail(error));

            var jobId = jobService.EnqueueImageGeneration(request);
            return Results.Accepted($"/api/jobs/{jobId}",
                ApiResponse<JobResponse>.Ok(jobService.MapJobToResponse(jobService.GetJob(jobId)!)));
        })
        .WithName("GenerateImageAsync")
        .WithDescription("Start async image generation. Poll GET /api/jobs/{job_id} for status and result.");

        // ── Batch image generation (always async) ──
        group.MapPost("/batch", async (
            BatchImageApiRequest request,
            JobService jobService,
            IFileService fileService,
            IDatabaseService dbService) =>
        {
            var error = Validation.ValidateBatchRequest(request);
            if (error != null)
                return Results.BadRequest(ApiResponse.Fail(error));

            var characters = new List<CharacterPromptDto>();

            if (request.Characters != null && request.Characters.Count > 0)
            {
                characters = request.Characters.Select(c => new CharacterPromptDto
                {
                    Name = c.Name,
                    FileName = c.FileName,
                    Prompt = c.Prompt,
                    NegativePrompt = c.NegativePrompt
                }).ToList();
            }
            else if (request.JsonFileId.HasValue)
            {
                var jsonFiles = await dbService.GetJsonFilesAsync();
                var jsonFile = jsonFiles.FirstOrDefault(f => f.Id == request.JsonFileId.Value);
                if (jsonFile == null)
                    return Results.NotFound(ApiResponse.Fail($"JSON file with id {request.JsonFileId} not found"));

                var prompts = await fileService.LoadPromptsFromJsonAsync(jsonFile.FilePath);
                characters = prompts.Select(p => new CharacterPromptDto
                {
                    Name = p.Name,
                    FileName = p.FileName,
                    Prompt = p.Prompt,
                    NegativePrompt = p.NegativePrompt
                }).ToList();
            }
            else
            {
                return Results.BadRequest(ApiResponse.Fail("Either 'characters' array or 'json_file_id' is required"));
            }

            if (characters.Count == 0)
                return Results.BadRequest(ApiResponse.Fail("No characters to process"));

            var jobId = jobService.EnqueueBatchImageGeneration(request, characters);
            return Results.Accepted($"/api/jobs/{jobId}",
                ApiResponse<JobResponse>.Ok(jobService.MapJobToResponse(jobService.GetJob(jobId)!)));
        })
        .WithName("BatchGenerateImages")
        .WithDescription("Start batch image generation for multiple characters. Always async. Poll GET /api/jobs/{job_id}.");

        // ── List generated images ──
        group.MapGet("/", async (IDatabaseService dbService) =>
        {
            var images = await dbService.GetImagesAsync();
            var response = images.Select(JobService.MapImageToResponse).ToList();
            return ApiResponse<List<ImageInfoResponse>>.Ok(response);
        })
        .WithName("ListImages")
        .WithDescription("List all generated images from the database");
    }
}
