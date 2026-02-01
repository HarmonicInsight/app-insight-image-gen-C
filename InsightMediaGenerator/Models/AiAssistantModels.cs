using System.Text.Json.Serialization;

namespace InsightMediaGenerator.Models;

/// <summary>
/// AI APIへ送信するリクエスト（OpenAI互換形式）
/// </summary>
public class AiChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "gpt-4o-mini";

    [JsonPropertyName("messages")]
    public List<AiChatMessage> Messages { get; set; } = new();

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 1024;

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;
}

public class AiChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// AI APIからのレスポンス（OpenAI互換形式）
/// </summary>
public class AiChatResponse
{
    [JsonPropertyName("choices")]
    public List<AiChatChoice> Choices { get; set; } = new();
}

public class AiChatChoice
{
    [JsonPropertyName("message")]
    public AiChatMessage Message { get; set; } = new();
}

/// <summary>
/// AIアシスタント設定
/// </summary>
public class AiAssistantConfig
{
    [JsonPropertyName("ai_assistant")]
    public AiEndpointConfig Endpoint { get; set; } = new();
}

public class AiEndpointConfig
{
    [JsonPropertyName("api_url")]
    public string ApiUrl { get; set; } = "https://api.openai.com/v1/chat/completions";

    [JsonPropertyName("api_key")]
    public string ApiKey { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = "gpt-4o-mini";
}
