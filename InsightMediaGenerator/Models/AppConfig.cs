using System.Text.Json.Serialization;

namespace InsightMediaGenerator.Models;

public class AppConfig
{
    [JsonPropertyName("app")]
    public AppInfo App { get; set; } = new();

    [JsonPropertyName("stable_diffusion")]
    public StableDiffusionConfig StableDiffusion { get; set; } = new();

    [JsonPropertyName("voicevox")]
    public VoicevoxConfig Voicevox { get; set; } = new();

    [JsonPropertyName("data")]
    public DataConfig Data { get; set; } = new();

    [JsonPropertyName("defaults")]
    public DefaultsConfig Defaults { get; set; } = new();
}

public class AppInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Insight Media Generator";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "2.0.0";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "Image and Audio Media Generation Tool";
}

public class StableDiffusionConfig
{
    [JsonPropertyName("api_url")]
    public string ApiUrl { get; set; } = "http://127.0.0.1:7860/sdapi/v1/txt2img";

    [JsonPropertyName("models_path")]
    public string ModelsPath { get; set; } = "";

    [JsonPropertyName("lora_path")]
    public string LoraPath { get; set; } = "";

    [JsonPropertyName("output_path")]
    public string OutputPath { get; set; } = "";
}

public class VoicevoxConfig
{
    [JsonPropertyName("api_url")]
    public string ApiUrl { get; set; } = "http://127.0.0.1:50021";

    [JsonPropertyName("auto_discover")]
    public bool AutoDiscover { get; set; } = true;

    [JsonPropertyName("output_path")]
    public string OutputPath { get; set; } = "./data/audio";
}

public class DataConfig
{
    [JsonPropertyName("json_upload_dir")]
    public string JsonUploadDir { get; set; } = "./data/json_files";

    [JsonPropertyName("database_file")]
    public string DatabaseFile { get; set; } = "./data/insight_media.db";
}

public class DefaultsConfig
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "dreamshaper_8.safetensors";

    [JsonPropertyName("sampler")]
    public string Sampler { get; set; } = "DPM++ 2M Karras";

    [JsonPropertyName("steps")]
    public int Steps { get; set; } = 30;

    [JsonPropertyName("cfg_scale")]
    public double CfgScale { get; set; } = 6;

    [JsonPropertyName("width")]
    public int Width { get; set; } = 768;

    [JsonPropertyName("height")]
    public int Height { get; set; } = 768;

    [JsonPropertyName("lora_weight")]
    public double LoraWeight { get; set; } = 0.8;

    [JsonPropertyName("speaker_id")]
    public int SpeakerId { get; set; } = 3;
}
