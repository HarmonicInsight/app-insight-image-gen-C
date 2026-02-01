namespace InsightMediaGenerator.Api.Endpoints;

public static class JobEndpoints
{
    public static void MapJobEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/jobs")
            .WithTags("Jobs");

        // ── Get job status ──
        group.MapGet("/{jobId}", (string jobId, JobService jobService) =>
        {
            var job = jobService.GetJob(jobId);
            if (job == null)
                return Results.NotFound(ApiResponse.Fail($"Job '{jobId}' not found"));

            return Results.Json(ApiResponse<JobResponse>.Ok(jobService.MapJobToResponse(job)));
        })
        .WithName("GetJob")
        .WithDescription("Get the status and result of an async job");

        // ── List all jobs ──
        group.MapGet("/", (JobService jobService) =>
        {
            var jobs = jobService.GetAllJobs();
            var response = new JobListResponse
            {
                Jobs = jobs.Select(j => jobService.MapJobToResponse(j)).ToList(),
                Total = jobs.Count
            };
            return ApiResponse<JobListResponse>.Ok(response);
        })
        .WithName("ListJobs")
        .WithDescription("List all jobs (queued, running, completed, failed)");

        // ── Cancel a job ──
        group.MapPost("/{jobId}/cancel", (string jobId, JobService jobService) =>
        {
            var cancelled = jobService.CancelJob(jobId);
            if (!cancelled)
                return Results.NotFound(ApiResponse.Fail($"Job '{jobId}' not found or already completed"));

            return Results.Json(ApiResponse.Ok());
        })
        .WithName("CancelJob")
        .WithDescription("Cancel a running or queued job");
    }
}
