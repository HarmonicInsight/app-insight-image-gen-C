namespace InsightMediaGenerator.Api;

public static class Validation
{
    public static string? ValidateImageRequest(ImageGenerateApiRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return "prompt is required";

        if (request.Steps.HasValue && (request.Steps < 1 || request.Steps > 150))
            return "steps must be between 1 and 150";

        if (request.CfgScale.HasValue && (request.CfgScale < 1 || request.CfgScale > 30))
            return "cfg_scale must be between 1 and 30";

        if (request.Width.HasValue && (request.Width < 64 || request.Width > 2048 || request.Width % 8 != 0))
            return "width must be between 64 and 2048, and a multiple of 8";

        if (request.Height.HasValue && (request.Height < 64 || request.Height > 2048 || request.Height % 8 != 0))
            return "height must be between 64 and 2048, and a multiple of 8";

        if (request.LoraWeight.HasValue && (request.LoraWeight < 0 || request.LoraWeight > 2.0))
            return "lora_weight must be between 0 and 2.0";

        if (request.BatchCount < 1 || request.BatchCount > 100)
            return "batch_count must be between 1 and 100";

        if (request.CharName.Contains("..") || request.CharName.Contains('/') || request.CharName.Contains('\\'))
            return "char_name must not contain path separators or '..'";

        return null;
    }

    public static string? ValidateAudioRequest(AudioGenerateApiRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return "text is required";

        if (request.Text.Length > 10000)
            return "text must not exceed 10000 characters";

        if (request.Speed < 0.5 || request.Speed > 2.0)
            return "speed must be between 0.5 and 2.0";

        if (request.Pitch < -0.15 || request.Pitch > 0.15)
            return "pitch must be between -0.15 and 0.15";

        if (request.Intonation < 0 || request.Intonation > 2.0)
            return "intonation must be between 0 and 2.0";

        if (request.Volume < 0 || request.Volume > 2.0)
            return "volume must be between 0 and 2.0";

        if (request.FileName != null && (request.FileName.Contains("..") || request.FileName.Contains('/') || request.FileName.Contains('\\')))
            return "file_name must not contain path separators or '..'";

        return null;
    }

    public static string? ValidateBatchRequest(BatchImageApiRequest request)
    {
        if (request.BatchCount < 1 || request.BatchCount > 100)
            return "batch_count must be between 1 and 100";

        if (request.Steps.HasValue && (request.Steps < 1 || request.Steps > 150))
            return "steps must be between 1 and 150";

        if (request.CfgScale.HasValue && (request.CfgScale < 1 || request.CfgScale > 30))
            return "cfg_scale must be between 1 and 30";

        if (request.Width.HasValue && (request.Width < 64 || request.Width > 2048 || request.Width % 8 != 0))
            return "width must be between 64 and 2048, and a multiple of 8";

        if (request.Height.HasValue && (request.Height < 64 || request.Height > 2048 || request.Height % 8 != 0))
            return "height must be between 64 and 2048, and a multiple of 8";

        if (request.Characters != null)
        {
            foreach (var c in request.Characters)
            {
                if (string.IsNullOrWhiteSpace(c.Prompt))
                    return $"character '{c.Name}' has empty prompt";
                if (c.FileName.Contains("..") || c.FileName.Contains('/') || c.FileName.Contains('\\'))
                    return $"character '{c.Name}' file_name must not contain path separators";
            }
        }

        return null;
    }

    public static string? ValidateUploadPrompt(UploadPromptFileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
            return "file_name is required";

        if (request.FileName.Contains("..") || request.FileName.Contains('/') || request.FileName.Contains('\\'))
            return "file_name must not contain path separators or '..'";

        if (request.Characters.Count == 0)
            return "characters array must not be empty";

        foreach (var c in request.Characters)
        {
            if (string.IsNullOrWhiteSpace(c.Name))
                return "each character must have a name";
            if (string.IsNullOrWhiteSpace(c.Prompt))
                return $"character '{c.Name}' must have a prompt";
        }

        return null;
    }
}
