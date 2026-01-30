using System.Collections.Concurrent;
using InsightMediaGenerator.Models;
using InsightMediaGenerator.Services.Interfaces;

namespace InsightMediaGenerator.Api;

public class JobInfo
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
}

public class JobService
{
    private readonly ConcurrentDictionary<string, JobInfo> _jobs = new();
    private readonly IStableDiffusionService _sdService;
    private readonly IVoicevoxService _voicevoxService;
    private readonly IDatabaseService _databaseService;
    private readonly IFileService _fileService;
    private readonly AppConfig _config;
    private readonly ILogger<JobService> _logger;

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
        _fileService = fileService;
        _config = config;
        _logger = logger;
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

            var genRequest = new ImageGenerationRequest
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

                if (i < totalSets - 1)
                    await Task.Delay(500, job.Cts.Token);
            }

            job.Status = JobStatus.Completed;
            job.Progress = 100;
            job.Message = $"Generated {allImages.Count} image(s)";
            job.Result = allImages;
            job.CompletedAt = DateTime.UtcNow;
        }
        catch (OperationCanceledException)
        {
            job.Status = JobStatus.Cancelled;
            job.Message = "Cancelled";
            job.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image generation job {JobId} failed", job.Id);
            job.Status = JobStatus.Failed;
            job.Message = ex.Message;
            job.CompletedAt = DateTime.UtcNow;
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
                        _logger.LogWarning("Batch generation failed for {CharName}: {Error}", character.Name, result.ErrorMessage);
                        continue;
                    }

                    foreach (var img in result.GeneratedImages)
                    {
                        await _databaseService.SaveImageMetadataAsync(img);
                        allImages.Add(MapImageToResponse(img));
                    }

                    await Task.Delay(500, job.Cts.Token);
                }
            }

            job.Status = JobStatus.Completed;
            job.Progress = 100;
            job.Message = $"Batch complete: {allImages.Count} image(s) generated";
            job.Result = allImages;
            job.CompletedAt = DateTime.UtcNow;
        }
        catch (OperationCanceledException)
        {
            job.Status = JobStatus.Cancelled;
            job.Message = "Cancelled";
            job.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch image generation job {JobId} failed", job.Id);
            job.Status = JobStatus.Failed;
            job.Message = ex.Message;
            job.CompletedAt = DateTime.UtcNow;
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

            job.Status = JobStatus.Completed;
            job.Progress = 100;
            job.Message = "Audio generation complete";
            job.Result = response;
            job.CompletedAt = DateTime.UtcNow;
        }
        catch (OperationCanceledException)
        {
            job.Status = JobStatus.Cancelled;
            job.Message = "Cancelled";
            job.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audio generation job {JobId} failed", job.Id);
            job.Status = JobStatus.Failed;
            job.Message = ex.Message;
            job.CompletedAt = DateTime.UtcNow;
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
                    var stepResult = await ExecutePipelineStepAsync(step, stepResults, job.Cts.Token);
                    stepStatus.Status = "completed";
                    stepStatus.Result = stepResult;
                }
                catch (Exception ex)
                {
                    stepStatus.Status = "failed";
                    stepStatus.Result = ex.Message;
                    _logger.LogWarning(ex, "Pipeline step {StepIndex} ({Action}) failed", i, step.Action);
                }

                stepResults.Add(stepStatus);
            }

            var pipelineResponse = new PipelineResponse
            {
                PipelineId = job.Id,
                Name = pipelineRequest.Name,
                Status = "completed",
                Steps = stepResults,
                CreatedAt = job.CreatedAt
            };

            job.Status = JobStatus.Completed;
            job.Progress = 100;
            job.Message = $"Pipeline '{pipelineRequest.Name}' complete";
            job.Result = pipelineResponse;
            job.CompletedAt = DateTime.UtcNow;
        }
        catch (OperationCanceledException)
        {
            job.Status = JobStatus.Cancelled;
            job.Message = "Pipeline cancelled";
            job.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipeline job {JobId} failed", job.Id);
            job.Status = JobStatus.Failed;
            job.Message = ex.Message;
            job.CompletedAt = DateTime.UtcNow;
        }
    }

    private async Task<object?> ExecutePipelineStepAsync(PipelineStep step, List<PipelineStepStatus> previousResults, CancellationToken ct)
    {
        var p = step.Params ?? new Dictionary<string, object>();

        switch (step.Action.ToLowerInvariant())
        {
            case "generate_image":
                return await PipelineGenerateImageAsync(p, ct);

            case "generate_audio":
                return await PipelineGenerateAudioAsync(p, ct);

            case "list_models":
                return await _sdService.GetModelsAsync();

            case "list_speakers":
                return await _voicevoxService.GetSpeakersAsync();

            case "check_status":
                var sd = await _sdService.CheckConnectionAsync();
                var vv = await _voicevoxService.CheckConnectionAsync();
                return new { stable_diffusion = sd, voicevox = vv };

            default:
                throw new InvalidOperationException($"Unknown pipeline action: {step.Action}");
        }
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
        if (!result.Success) throw new Exception(result.ErrorMessage);

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
        if (!result.Success) throw new Exception(result.ErrorMessage);

        return new AudioGenerateApiResponse
        {
            FilePath = result.FilePath,
            FileSizeBytes = result.AudioData?.Length
        };
    }

    // ── Helpers ──

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

    private static T? GetParam<T>(Dictionary<string, object> p, string key)
    {
        if (!p.TryGetValue(key, out var value)) return default;
        if (value is T typed) return typed;
        try
        {
            if (value is System.Text.Json.JsonElement je)
            {
                var json = je.GetRawText();
                return System.Text.Json.JsonSerializer.Deserialize<T>(json);
            }
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch { return default; }
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
        Result = job.Status == JobStatus.Completed ? job.Result : null
    };
}
