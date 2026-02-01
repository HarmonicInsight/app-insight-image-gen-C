using System.Collections.Concurrent;
using InsightMediaGenerator.Models;
using InsightMediaGenerator.Services.Interfaces;

namespace InsightMediaGenerator.Api;

public class JobInfo : IDisposable
{
    public string Id { get; set; } = string.Empty;
    public JobType Type { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Queued;
    public double Progress { get; set; }
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public object? Result { get; set; }
    public CancellationTokenSource Cts { get; set; } = new();
    public int FailedCount { get; set; }
    public int SuccessCount { get; set; }

    public void Dispose()
    {
        Cts.Dispose();
    }
}

public class JobService : IDisposable
{
    private readonly ConcurrentDictionary<string, JobInfo> _jobs = new();
    private readonly IStableDiffusionService _sdService;
    private readonly IVoicevoxService _voicevoxService;
    private readonly IDatabaseService _databaseService;
    private readonly AppConfig _config;
    private readonly ILogger<JobService> _logger;
    private readonly Timer _cleanupTimer;

    private const int MaxJobRetentionMinutes = 60;
    private const int CleanupIntervalMinutes = 5;

    public JobService(
        IStableDiffusionService sdService,
        IVoicevoxService voicevoxService,
        IDatabaseService databaseService,
        IFileService fileService,
        AppConfig config,
        ILogger<JobService> logger)
    {
        _sdService = sdService;
        _voicevoxService = voicevoxService;
        _databaseService = databaseService;
        _config = config;
        _logger = logger;

        _cleanupTimer = new Timer(CleanupExpiredJobs, null,
            TimeSpan.FromMinutes(CleanupIntervalMinutes),
            TimeSpan.FromMinutes(CleanupIntervalMinutes));
    }

    private void CleanupExpiredJobs(object? state)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-MaxJobRetentionMinutes);
        var expiredIds = _jobs
            .Where(kv => kv.Value.CompletedAt.HasValue && kv.Value.CompletedAt.Value < cutoff)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var id in expiredIds)
        {
            if (_jobs.TryRemove(id, out var removed))
            {
                removed.Dispose();
                _logger.LogDebug("Cleaned up expired job {JobId}", id);
            }
        }

        if (expiredIds.Count > 0)
            _logger.LogInformation("Job cleanup: removed {Count} expired job(s), {Remaining} remaining",
                expiredIds.Count, _jobs.Count);
    }

    public JobInfo? GetJob(string jobId) => _jobs.GetValueOrDefault(jobId);

    public IReadOnlyList<JobInfo> GetAllJobs() => _jobs.Values.OrderByDescending(j => j.CreatedAt).ToList();

    public bool CancelJob(string jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job) && job.Status is JobStatus.Queued or JobStatus.Running)
        {
            job.Cts.Cancel();
            job.Status = JobStatus.Cancelled;
            job.CompletedAt = DateTime.UtcNow;
            job.Message = "Cancelled by user";
            return true;
        }
        return false;
    }

    // ── Image Generation Job ──

    public string EnqueueImageGeneration(ImageGenerateApiRequest request)
    {
        var job = CreateJob(JobType.ImageGeneration);
        _ = Task.Run(() => RunImageGenerationAsync(job, request));
        return job.Id;
    }

    private async Task RunImageGenerationAsync(JobInfo job, ImageGenerateApiRequest request)
    {
        try
        {
            job.Status = JobStatus.Running;
            job.Message = "Generating image...";

            var genRequest = BuildImageRequest(request);
            var totalSets = request.BatchCount;
            var allImages = new List<ImageInfoResponse>();

            for (int i = 0; i < totalSets; i++)
            {
                job.Progress = (double)i / totalSets * 100;
                job.Message = $"Generating set {i + 1}/{totalSets}...";

                var result = await _sdService.GenerateAsync(genRequest, job.Cts.Token);
                if (!result.Success)
                {
                    job.Status = JobStatus.Failed;
                    job.Message = result.ErrorMessage;
                    job.CompletedAt = DateTime.UtcNow;
                    return;
                }

                foreach (var img in result.GeneratedImages)
                {
                    await _databaseService.SaveImageMetadataAsync(img);
                    allImages.Add(MapImageToResponse(img));
                }
                job.SuccessCount = allImages.Count;

                if (i < totalSets - 1)
                    await Task.Delay(500, job.Cts.Token);
            }

            CompleteJob(job, allImages, $"Generated {allImages.Count} image(s)");
        }
        catch (OperationCanceledException)
        {
            MarkCancelled(job);
        }
        catch (Exception ex)
        {
            MarkFailed(job, ex, "Image generation");
        }
    }

    // ── Batch Image Generation Job ──

    public string EnqueueBatchImageGeneration(BatchImageApiRequest request, List<CharacterPromptDto> characters)
    {
        var job = CreateJob(JobType.BatchImageGeneration);
        _ = Task.Run(() => RunBatchImageGenerationAsync(job, request, characters));
        return job.Id;
    }

    private async Task RunBatchImageGenerationAsync(JobInfo job, BatchImageApiRequest request, List<CharacterPromptDto> characters)
    {
        try
        {
            job.Status = JobStatus.Running;
            var allImages = new List<ImageInfoResponse>();
            var totalOps = characters.Count * request.BatchCount;
            var currentOp = 0;
            var errors = new List<string>();

            foreach (var character in characters)
            {
                for (int i = 0; i < request.BatchCount; i++)
                {
                    currentOp++;
                    job.Progress = (double)currentOp / totalOps * 100;
                    job.Message = $"Generating {character.Name} ({currentOp}/{totalOps})...";

                    var genRequest = new ImageGenerationRequest
                    {
                        Prompt = character.Prompt,
                        NegativePrompt = character.NegativePrompt,
                        Model = request.Model ?? _config.Defaults.Model,
                        Lora = request.Lora,
                        LoraWeight = request.LoraWeight ?? _config.Defaults.LoraWeight,
                        Steps = request.Steps ?? _config.Defaults.Steps,
                        CfgScale = request.CfgScale ?? _config.Defaults.CfgScale,
                        Width = request.Width ?? _config.Defaults.Width,
                        Height = request.Height ?? _config.Defaults.Height,
                        SamplerName = request.Sampler ?? _config.Defaults.Sampler,
                        CharName = character.FileName,
                        JsonFileId = request.JsonFileId,
                        BatchCount = 1
                    };

                    var result = await _sdService.GenerateAsync(genRequest, job.Cts.Token);
                    if (!result.Success)
                    {
                        job.FailedCount++;
                        errors.Add($"{character.Name}: {result.ErrorMessage}");
                        _logger.LogWarning("Batch generation failed for {CharName}: {Error}", character.Name, result.ErrorMessage);
                        continue;
                    }

                    foreach (var img in result.GeneratedImages)
                    {
                        await _databaseService.SaveImageMetadataAsync(img);
                        allImages.Add(MapImageToResponse(img));
                    }
                    job.SuccessCount = allImages.Count;

                    await Task.Delay(500, job.Cts.Token);
                }
            }

            // Distinguish full success from partial success
            if (errors.Count == 0)
            {
                CompleteJob(job, allImages, $"Batch complete: {allImages.Count} image(s) generated");
            }
            else if (allImages.Count > 0)
            {
                job.Status = JobStatus.Completed;
                job.Progress = 100;
                job.Message = $"Batch partial: {allImages.Count} succeeded, {job.FailedCount} failed";
                job.Result = new { images = allImages, errors };
                job.CompletedAt = DateTime.UtcNow;
            }
            else
            {
                job.Status = JobStatus.Failed;
                job.Message = $"Batch failed: all {totalOps} operations failed";
                job.Result = new { errors };
                job.CompletedAt = DateTime.UtcNow;
            }
        }
        catch (OperationCanceledException)
        {
            MarkCancelled(job);
        }
        catch (Exception ex)
        {
            MarkFailed(job, ex, "Batch image generation");
        }
    }

    // ── Audio Generation Job ──

    public string EnqueueAudioGeneration(AudioGenerateApiRequest request)
    {
        var job = CreateJob(JobType.AudioGeneration);
        _ = Task.Run(() => RunAudioGenerationAsync(job, request));
        return job.Id;
    }

    private async Task RunAudioGenerationAsync(JobInfo job, AudioGenerateApiRequest request)
    {
        try
        {
            job.Status = JobStatus.Running;
            job.Message = "Generating audio...";

            var audioParams = new AudioGenerationParams
            {
                Text = request.Text,
                SpeakerId = request.SpeakerId ?? _config.Defaults.SpeakerId,
                Speed = request.Speed,
                Pitch = request.Pitch,
                Intonation = request.Intonation,
                Volume = request.Volume,
                SaveFile = request.SaveFile,
                FileName = request.FileName
            };

            var result = await _voicevoxService.GenerateAudioAsync(audioParams, job.Cts.Token);

            if (!result.Success)
            {
                job.Status = JobStatus.Failed;
                job.Message = result.ErrorMessage;
                job.CompletedAt = DateTime.UtcNow;
                return;
            }

            var response = new AudioGenerateApiResponse
            {
                FilePath = result.FilePath,
                FileSizeBytes = result.AudioData?.Length
            };

            CompleteJob(job, response, "Audio generation complete");
        }
        catch (OperationCanceledException)
        {
            MarkCancelled(job);
        }
        catch (Exception ex)
        {
            MarkFailed(job, ex, "Audio generation");
        }
    }

    // ── Pipeline Execution ──

    public string EnqueuePipeline(PipelineRequest request)
    {
        var job = CreateJob(JobType.BatchImageGeneration);
        job.Message = $"Pipeline: {request.Name}";
        _ = Task.Run(() => RunPipelineAsync(job, request));
        return job.Id;
    }

    private async Task RunPipelineAsync(JobInfo job, PipelineRequest pipelineRequest)
    {
        try
        {
            job.Status = JobStatus.Running;
            var stepResults = new List<PipelineStepStatus>();

            for (int i = 0; i < pipelineRequest.Steps.Count; i++)
            {
                var step = pipelineRequest.Steps[i];
                job.Progress = (double)i / pipelineRequest.Steps.Count * 100;
                job.Message = $"Pipeline step {i + 1}/{pipelineRequest.Steps.Count}: {step.Action}";

                var stepStatus = new PipelineStepStatus
                {
                    Index = i,
                    Action = step.Action,
                    Status = "running"
                };

                try
                {
                    var stepResult = await ExecutePipelineStepAsync(step, job.Cts.Token);
                    stepStatus.Status = "completed";
                    stepStatus.Result = stepResult;
                    job.SuccessCount++;
                }
                catch (Exception ex)
                {
                    stepStatus.Status = "failed";
                    stepStatus.Result = ex.Message;
                    job.FailedCount++;
                    _logger.LogWarning(ex, "Pipeline step {StepIndex} ({Action}) failed", i, step.Action);
                }

                stepResults.Add(stepStatus);
            }

            var hasFailures = stepResults.Any(s => s.Status == "failed");
            var pipelineStatus = hasFailures ? "completed_with_errors" : "completed";

            var pipelineResponse = new PipelineResponse
            {
                PipelineId = job.Id,
                Name = pipelineRequest.Name,
                Status = pipelineStatus,
                Steps = stepResults,
                CreatedAt = job.CreatedAt
            };

            job.Status = JobStatus.Completed;
            job.Progress = 100;
            job.Message = hasFailures
                ? $"Pipeline '{pipelineRequest.Name}': {job.SuccessCount} succeeded, {job.FailedCount} failed"
                : $"Pipeline '{pipelineRequest.Name}' complete ({job.SuccessCount} steps)";
            job.Result = pipelineResponse;
            job.CompletedAt = DateTime.UtcNow;
        }
        catch (OperationCanceledException)
        {
            MarkCancelled(job);
        }
        catch (Exception ex)
        {
            MarkFailed(job, ex, "Pipeline");
        }
    }

    private async Task<object?> ExecutePipelineStepAsync(PipelineStep step, CancellationToken ct)
    {
        var p = step.Params ?? new Dictionary<string, object>();

        return step.Action.ToLowerInvariant() switch
        {
            "generate_image" => await PipelineGenerateImageAsync(p, ct),
            "generate_audio" => await PipelineGenerateAudioAsync(p, ct),
            "list_models" => await _sdService.GetModelsAsync(),
            "list_speakers" => await _voicevoxService.GetSpeakersAsync(),
            "check_status" => new
            {
                stable_diffusion = await _sdService.CheckConnectionAsync(),
                voicevox = await _voicevoxService.CheckConnectionAsync()
            },
            _ => throw new InvalidOperationException($"Unknown pipeline action: {step.Action}")
        };
    }

    private async Task<object> PipelineGenerateImageAsync(Dictionary<string, object> p, CancellationToken ct)
    {
        var request = new ImageGenerationRequest
        {
            Prompt = GetParam<string>(p, "prompt") ?? "",
            NegativePrompt = GetParam<string>(p, "negative_prompt") ?? "",
            Model = GetParam<string>(p, "model") ?? _config.Defaults.Model,
            Lora = GetParam<string>(p, "lora"),
            LoraWeight = GetParam<double?>(p, "lora_weight") ?? _config.Defaults.LoraWeight,
            Steps = GetParam<int?>(p, "steps") ?? _config.Defaults.Steps,
            CfgScale = GetParam<double?>(p, "cfg_scale") ?? _config.Defaults.CfgScale,
            Width = GetParam<int?>(p, "width") ?? _config.Defaults.Width,
            Height = GetParam<int?>(p, "height") ?? _config.Defaults.Height,
            SamplerName = GetParam<string>(p, "sampler") ?? _config.Defaults.Sampler,
            CharName = GetParam<string>(p, "char_name") ?? "pipeline",
            BatchCount = 1
        };

        var result = await _sdService.GenerateAsync(request, ct);
        if (!result.Success)
            throw new InvalidOperationException($"Image generation failed: {result.ErrorMessage}");

        foreach (var img in result.GeneratedImages)
            await _databaseService.SaveImageMetadataAsync(img);

        return result.GeneratedImages.Select(MapImageToResponse).ToList();
    }

    private async Task<object> PipelineGenerateAudioAsync(Dictionary<string, object> p, CancellationToken ct)
    {
        var audioParams = new AudioGenerationParams
        {
            Text = GetParam<string>(p, "text") ?? "",
            SpeakerId = GetParam<int?>(p, "speaker_id") ?? _config.Defaults.SpeakerId,
            Speed = GetParam<double?>(p, "speed") ?? 1.0,
            Pitch = GetParam<double?>(p, "pitch") ?? 0.0,
            Intonation = GetParam<double?>(p, "intonation") ?? 1.0,
            Volume = GetParam<double?>(p, "volume") ?? 1.0,
            SaveFile = GetParam<bool?>(p, "save_file") ?? true,
            FileName = GetParam<string>(p, "file_name")
        };

        var result = await _voicevoxService.GenerateAudioAsync(audioParams, ct);
        if (!result.Success)
            throw new InvalidOperationException($"Audio generation failed: {result.ErrorMessage}");

        return new AudioGenerateApiResponse
        {
            FilePath = result.FilePath,
            FileSizeBytes = result.AudioData?.Length
        };
    }

    // ── Helpers ──

    private ImageGenerationRequest BuildImageRequest(ImageGenerateApiRequest request) => new()
    {
        Prompt = request.Prompt,
        NegativePrompt = request.NegativePrompt,
        Model = request.Model ?? _config.Defaults.Model,
        Lora = request.Lora,
        LoraWeight = request.LoraWeight ?? _config.Defaults.LoraWeight,
        Steps = request.Steps ?? _config.Defaults.Steps,
        CfgScale = request.CfgScale ?? _config.Defaults.CfgScale,
        Width = request.Width ?? _config.Defaults.Width,
        Height = request.Height ?? _config.Defaults.Height,
        SamplerName = request.Sampler ?? _config.Defaults.Sampler,
        CharName = request.CharName,
        BatchCount = request.BatchCount
    };

    private JobInfo CreateJob(JobType type)
    {
        var job = new JobInfo
        {
            Id = $"job_{Guid.NewGuid():N}",
            Type = type,
            CreatedAt = DateTime.UtcNow
        };
        _jobs[job.Id] = job;
        return job;
    }

    private static void CompleteJob(JobInfo job, object result, string message)
    {
        job.Status = JobStatus.Completed;
        job.Progress = 100;
        job.Message = message;
        job.Result = result;
        job.CompletedAt = DateTime.UtcNow;
    }

    private static void MarkCancelled(JobInfo job)
    {
        job.Status = JobStatus.Cancelled;
        job.Message = "Cancelled";
        job.CompletedAt = DateTime.UtcNow;
    }

    private void MarkFailed(JobInfo job, Exception ex, string context)
    {
        _logger.LogError(ex, "{Context} job {JobId} failed", context, job.Id);
        job.Status = JobStatus.Failed;
        job.Message = ex.Message;
        job.CompletedAt = DateTime.UtcNow;
    }

    private static T? GetParam<T>(Dictionary<string, object> p, string key)
    {
        if (!p.TryGetValue(key, out var value)) return default;
        if (value is T typed) return typed;

        if (value is System.Text.Json.JsonElement je)
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(je.GetRawText());
        }

        return (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
    }

    public static ImageInfoResponse MapImageToResponse(ImageMetadata img) => new()
    {
        Id = img.Id,
        FileName = img.FileName,
        FilePath = img.FilePath,
        Timestamp = img.Timestamp,
        Model = img.Model,
        Lora = img.Lora,
        LoraWeight = img.LoraWeight,
        Prompt = img.Prompt,
        NegativePrompt = img.NegativePrompt,
        Steps = img.Steps,
        Width = img.Width,
        Height = img.Height,
        Sampler = img.SamplerName,
        CfgScale = img.CfgScale,
        CharName = img.CharName,
        BatchIndex = img.BatchIndex
    };

    public JobResponse MapJobToResponse(JobInfo job) => new()
    {
        JobId = job.Id,
        Type = job.Type.ToString(),
        Status = job.Status.ToString(),
        Progress = job.Progress,
        Message = job.Message,
        CreatedAt = job.CreatedAt,
        CompletedAt = job.CompletedAt,
        Result = job.Status is JobStatus.Completed or JobStatus.Failed ? job.Result : null
    };

    public void Dispose()
    {
        _cleanupTimer.Dispose();
        foreach (var job in _jobs.Values)
            job.Dispose();
        _jobs.Clear();
    }
}
