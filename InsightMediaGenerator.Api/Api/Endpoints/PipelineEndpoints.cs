namespace InsightMediaGenerator.Api.Endpoints;

public static class PipelineEndpoints
{
    public static void MapPipelineEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/pipelines")
            .WithTags("Pipelines");

        // ── Execute a pipeline (multi-step automation) ──
        group.MapPost("/", (PipelineRequest request, JobService jobService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest(ApiResponse.Fail("name is required"));
            if (request.Steps.Count == 0)
                return Results.BadRequest(ApiResponse.Fail("steps array must not be empty"));

            var validActions = new HashSet<string>
            {
                "generate_image", "generate_audio",
                "list_models", "list_speakers", "check_status"
            };

            foreach (var step in request.Steps)
            {
                if (!validActions.Contains(step.Action.ToLowerInvariant()))
                    return Results.BadRequest(ApiResponse.Fail($"Unknown action: '{step.Action}'. Valid: {string.Join(", ", validActions)}"));
            }

            var jobId = jobService.EnqueuePipeline(request);
            return Results.Accepted($"/api/jobs/{jobId}",
                ApiResponse<JobResponse>.Ok(jobService.MapJobToResponse(jobService.GetJob(jobId)!)));
        })
        .WithName("ExecutePipeline")
        .WithDescription("Execute a multi-step pipeline. Each step runs sequentially. Poll /api/jobs/{job_id} for status.");
    }
}
