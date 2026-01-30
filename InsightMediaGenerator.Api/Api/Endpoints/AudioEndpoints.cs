using InsightMediaGenerator.Models;
using InsightMediaGenerator.Services.Interfaces;

namespace InsightMediaGenerator.Api.Endpoints;

public static class AudioEndpoints
{
    public static void MapAudioEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/audio")
            .WithTags("Audio");

        // ── Synchronous audio generation ──
        group.MapPost("/generate", async (
            AudioGenerateApiRequest request,
            IVoicevoxService vvService,
            AppConfig config,
            CancellationToken ct) =>
        {
            var error = Validation.ValidateAudioRequest(request);
            if (error != null)
                return Results.BadRequest(ApiResponse.Fail(error));

            var audioParams = new AudioGenerationParams
            {
                Text = request.Text,
                SpeakerId = request.SpeakerId ?? config.Defaults.SpeakerId,
                Speed = request.Speed,
                Pitch = request.Pitch,
                Intonation = request.Intonation,
                Volume = request.Volume,
                SaveFile = request.SaveFile,
                FileName = request.FileName
            };

            var result = await vvService.GenerateAudioAsync(audioParams, ct);

            if (!result.Success)
                return Results.Json(
                    ApiResponse<AudioGenerateApiResponse>.Fail(result.ErrorMessage ?? "Audio generation failed"),
                    statusCode: StatusCodes.Status502BadGateway);

            var response = new AudioGenerateApiResponse
            {
                FilePath = result.FilePath,
                FileSizeBytes = result.AudioData?.Length
            };

            return Results.Json(ApiResponse<AudioGenerateApiResponse>.Ok(response));
        })
        .WithName("GenerateAudio")
        .WithDescription("Generate audio synchronously. Blocks until complete.");

        // ── Async audio generation ──
        group.MapPost("/generate/async", (
            AudioGenerateApiRequest request,
            JobService jobService) =>
        {
            var error = Validation.ValidateAudioRequest(request);
            if (error != null)
                return Results.BadRequest(ApiResponse.Fail(error));

            var jobId = jobService.EnqueueAudioGeneration(request);
            return Results.Accepted($"/api/jobs/{jobId}",
                ApiResponse<JobResponse>.Ok(jobService.MapJobToResponse(jobService.GetJob(jobId)!)));
        })
        .WithName("GenerateAudioAsync")
        .WithDescription("Start async audio generation. Poll /api/jobs/{job_id} for status.");

        // ── List speakers ──
        group.MapGet("/speakers", async (IVoicevoxService vvService) =>
        {
            var speakers = await vvService.GetSpeakersAsync();
            var response = speakers.Select(s => new SpeakerResponse
            {
                Id = s.Id,
                Name = s.Name,
                SpeakerName = s.SpeakerName,
                StyleName = s.StyleName
            }).ToList();

            return ApiResponse<List<SpeakerResponse>>.Ok(response);
        })
        .WithName("GetSpeakers")
        .WithDescription("List available VOICEVOX speakers/voices");
    }
}
