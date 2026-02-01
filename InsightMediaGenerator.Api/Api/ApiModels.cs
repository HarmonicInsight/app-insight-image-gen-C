using System.Text.Json.Serialization;

namespace InsightMediaGenerator.Api;

// ──────────────────────────────────────────
// Common Response Envelope
// ──────────────────────────────────────────

public class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };
    public static ApiResponse<T> Fail(string error) => new() { Success = false, Error = error };
}

public class ApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse Ok() => new() { Success = true };
    public static ApiResponse Fail(string error) => new() { Success = false, Error = error };
}

// ──────────────────────────────────────────
// Health / Status
// ──────────────────────────────────────────

public class HealthResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "ok";

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("uptime_seconds")]
    public long UptimeSeconds { get; set; }
}

public class ConnectionStatus
{
    [JsonPropertyName("stable_diffusion")]
    public ServiceStatus StableDiffusion { get; set; } = new();

    [JsonPropertyName("voicevox")]
    public ServiceStatus Voicevox { get; set; } = new();
}

public class ServiceStatus
{
    [JsonPropertyName("connected")]
    public bool Connected { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

// ──────────────────────────────────────────
// Image Generation API
// ──────────────────────────────────────────

public class ImageGenerateApiRequest
{
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("negative_prompt")]
    public string NegativePrompt { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("lora")]
    public string? Lora { get; set; }

    [JsonPropertyName("lora_weight")]
    public double? LoraWeight { get; set; }

    [JsonPropertyName("steps")]
    public int? Steps { get; set; }

    [JsonPropertyName("cfg_scale")]
    public double? CfgScale { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("sampler")]
    public string? Sampler { get; set; }

    [JsonPropertyName("char_name")]
    public string CharName { get; set; } = "image";

    [JsonPropertyName("batch_count")]
    public int BatchCount { get; set; } = 1;
}

public class BatchImageApiRequest
{
    [JsonPropertyName("json_file_id")]
    public int? JsonFileId { get; set; }

    [JsonPropertyName("characters")]
    public List<BatchCharacterRequest>? Characters { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("lora")]
    public string? Lora { get; set; }

    [JsonPropertyName("lora_weight")]
    public double? LoraWeight { get; set; }

    [JsonPropertyName("steps")]
    public int? Steps { get; set; }

    [JsonPropertyName("cfg_scale")]
    public double? CfgScale { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("sampler")]
    public string? Sampler { get; set; }

    [JsonPropertyName("batch_count")]
    public int BatchCount { get; set; } = 1;
}

public class BatchCharacterRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("negative_prompt")]
    public string NegativePrompt { get; set; } = string.Empty;
}

public class ImageInfoResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("file_path")]
    public string FilePath { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("lora")]
    public string? Lora { get; set; }

    [JsonPropertyName("lora_weight")]
    public double LoraWeight { get; set; }

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("negative_prompt")]
    public string NegativePrompt { get; set; } = string.Empty;

    [JsonPropertyName("steps")]
    public int Steps { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("sampler")]
    public string Sampler { get; set; } = string.Empty;

    [JsonPropertyName("cfg_scale")]
    public double CfgScale { get; set; }

    [JsonPropertyName("char_name")]
    public string CharName { get; set; } = string.Empty;

    [JsonPropertyName("batch_index")]
    public int BatchIndex { get; set; }
}

// ──────────────────────────────────────────
// Audio Generation API
// ──────────────────────────────────────────

public class AudioGenerateApiRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("speaker_id")]
    public int? SpeakerId { get; set; }

    [JsonPropertyName("speed")]
    public double Speed { get; set; } = 1.0;

    [JsonPropertyName("pitch")]
    public double Pitch { get; set; } = 0.0;

    [JsonPropertyName("intonation")]
    public double Intonation { get; set; } = 1.0;

    [JsonPropertyName("volume")]
    public double Volume { get; set; } = 1.0;

    [JsonPropertyName("save_file")]
    public bool SaveFile { get; set; } = true;

    [JsonPropertyName("file_name")]
    public string? FileName { get; set; }
}

public class AudioGenerateApiResponse
{
    [JsonPropertyName("file_path")]
    public string? FilePath { get; set; }

    [JsonPropertyName("file_size_bytes")]
    public long? FileSizeBytes { get; set; }
}

public class SpeakerResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("speaker_name")]
    public string SpeakerName { get; set; } = string.Empty;

    [JsonPropertyName("style_name")]
    public string StyleName { get; set; } = string.Empty;
}

// ──────────────────────────────────────────
// Prompt / JSON File Management
// ──────────────────────────────────────────

public class PromptFileResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("file_path")]
    public string FilePath { get; set; } = string.Empty;

    [JsonPropertyName("uploaded_at")]
    public DateTime UploadedAt { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }
}

public class UploadPromptFileRequest
{
    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("characters")]
    public List<CharacterPromptDto> Characters { get; set; } = new();

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }
}

public class CharacterPromptDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("negative_prompt")]
    public string NegativePrompt { get; set; } = string.Empty;
}

// ──────────────────────────────────────────
// Job Tracking (Async Operations)
// ──────────────────────────────────────────

public enum JobStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled
}

public enum JobType
{
    ImageGeneration,
    BatchImageGeneration,
    AudioGeneration
}

public class JobResponse
{
    [JsonPropertyName("job_id")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("progress")]
    public double Progress { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [JsonPropertyName("result")]
    public object? Result { get; set; }
}

public class JobListResponse
{
    [JsonPropertyName("jobs")]
    public List<JobResponse> Jobs { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

// ──────────────────────────────────────────
// Pipeline (Multi-step automation)
// ──────────────────────────────────────────

public class PipelineRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("steps")]
    public List<PipelineStep> Steps { get; set; } = new();
}

public class PipelineStep
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    public Dictionary<string, object>? Params { get; set; }
}

public class PipelineResponse
{
    [JsonPropertyName("pipeline_id")]
    public string PipelineId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("steps")]
    public List<PipelineStepStatus> Steps { get; set; } = new();

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

public class PipelineStepStatus
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("job_id")]
    public string? JobId { get; set; }

    [JsonPropertyName("result")]
    public object? Result { get; set; }
}
