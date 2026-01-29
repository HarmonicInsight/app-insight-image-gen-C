using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InsightMediaGenerator.Models;
using InsightMediaGenerator.Services.Interfaces;

namespace InsightMediaGenerator.ViewModels;

public partial class BatchImageViewModel : ObservableObject
{
    private readonly IStableDiffusionService _sdService;
    private readonly IDatabaseService _dbService;
    private readonly IFileService _fileService;
    private readonly AppConfig _config;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private string? _selectedModel;

    [ObservableProperty]
    private string _selectedLora = "None";

    [ObservableProperty]
    private double _loraWeight = 0.8;

    [ObservableProperty]
    private int _steps = 30;

    [ObservableProperty]
    private double _cfgScale = 6.0;

    [ObservableProperty]
    private string _selectedResolution = "768x768";

    [ObservableProperty]
    private string _selectedSampler = "DPM++ 2M Karras";

    [ObservableProperty]
    private int _batchCount = 1;

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private bool _sdConnected;

    [ObservableProperty]
    private string _statusMessage = "Checking...";

    [ObservableProperty]
    private string _progressMessage = string.Empty;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private JsonFileRecord? _selectedJsonFile;

    [ObservableProperty]
    private string _jsonSearchText = string.Empty;

    [ObservableProperty]
    private bool _selectAllCharacters = true;

    public ObservableCollection<string> Models { get; } = new();
    public ObservableCollection<string> Loras { get; } = new();
    public ObservableCollection<JsonFileRecord> JsonFiles { get; } = new();
    public ObservableCollection<CharacterPrompt> Characters { get; } = new();

    public ObservableCollection<string> Resolutions { get; } = new()
    {
        "512x512", "768x768", "1024x1024"
    };

    public ObservableCollection<string> Samplers { get; } = new()
    {
        "DPM++ 2M Karras", "Euler a", "Heun"
    };

    public ObservableCollection<int> BatchCounts { get; } = new() { 1, 2, 3, 5, 10 };

    public BatchImageViewModel(
        IStableDiffusionService sdService,
        IDatabaseService dbService,
        IFileService fileService,
        AppConfig config)
    {
        _sdService = sdService;
        _dbService = dbService;
        _fileService = fileService;
        _config = config;

        // Apply defaults
        Steps = config.Defaults.Steps;
        CfgScale = config.Defaults.CfgScale;
        LoraWeight = config.Defaults.LoraWeight;
        SelectedSampler = config.Defaults.Sampler;
        SelectedResolution = $"{config.Defaults.Width}x{config.Defaults.Height}";
    }

    public async Task InitializeAsync()
    {
        await CheckStatusAsync();
        await LoadModelsAsync();
        await LoadLorasAsync();
        await LoadJsonFilesAsync();
    }

    [RelayCommand]
    private async Task CheckStatusAsync()
    {
        StatusMessage = "Checking...";
        SdConnected = await _sdService.CheckConnectionAsync();
        StatusMessage = SdConnected ? "Connected" : "Disconnected";
    }

    private async Task LoadModelsAsync()
    {
        var models = await _sdService.GetModelsAsync();
        Models.Clear();
        foreach (var model in models)
            Models.Add(model);

        if (Models.Count > 0)
        {
            SelectedModel = Models.Contains(_config.Defaults.Model)
                ? _config.Defaults.Model
                : Models[0];
        }
    }

    private async Task LoadLorasAsync()
    {
        var loras = await _sdService.GetLorasAsync();
        Loras.Clear();
        Loras.Add("None");
        foreach (var lora in loras)
            Loras.Add(lora);
    }

    [RelayCommand]
    private async Task LoadJsonFilesAsync()
    {
        var files = await _dbService.GetJsonFilesAsync();
        JsonFiles.Clear();
        foreach (var file in files)
            JsonFiles.Add(file);
    }

    [RelayCommand]
    private async Task UploadJsonFileAsync(string filePath)
    {
        try
        {
            var fileName = Path.GetFileName(filePath);
            using var stream = File.OpenRead(filePath);

            var savedPath = await _fileService.SaveJsonFileAsync(fileName, stream);

            var record = new JsonFileRecord
            {
                FileName = fileName,
                FilePath = savedPath,
                UploadedAt = DateTime.Now
            };

            await _dbService.SaveJsonFileAsync(record);
            await LoadJsonFilesAsync();

            ProgressMessage = $"Uploaded: {fileName}";
        }
        catch (Exception ex)
        {
            ProgressMessage = $"Upload failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteJsonFileAsync(JsonFileRecord? record)
    {
        if (record == null) return;

        try
        {
            await _fileService.DeleteJsonFileAsync(record.FilePath);
            await _dbService.DeleteJsonFileAsync(record.Id);
            await LoadJsonFilesAsync();

            if (SelectedJsonFile?.Id == record.Id)
            {
                SelectedJsonFile = null;
                Characters.Clear();
            }

            ProgressMessage = $"Deleted: {record.FileName}";
        }
        catch (Exception ex)
        {
            ProgressMessage = $"Delete failed: {ex.Message}";
        }
    }

    partial void OnSelectedJsonFileChanged(JsonFileRecord? value)
    {
        _ = LoadCharactersAsync();
    }

    private async Task LoadCharactersAsync()
    {
        Characters.Clear();

        if (SelectedJsonFile == null) return;

        var prompts = await _fileService.LoadPromptsFromJsonAsync(SelectedJsonFile.FilePath);
        foreach (var prompt in prompts)
        {
            prompt.IsSelected = SelectAllCharacters;
            Characters.Add(prompt);
        }
    }

    partial void OnSelectAllCharactersChanged(bool value)
    {
        foreach (var character in Characters)
        {
            character.IsSelected = value;
        }
    }

    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private async Task GenerateBatchAsync()
    {
        var selectedChars = Characters.Where(c => c.IsSelected).ToList();
        if (selectedChars.Count == 0 || string.IsNullOrEmpty(SelectedModel))
            return;

        IsGenerating = true;
        _cts = new CancellationTokenSource();
        Progress = 0;

        try
        {
            var (width, height) = ParseResolution(SelectedResolution);
            var total = selectedChars.Count * BatchCount;
            var current = 0;

            foreach (var character in selectedChars)
            {
                for (int i = 0; i < BatchCount; i++)
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    ProgressMessage = $"Generating {character.Name} ({current + 1}/{total})...";

                    var request = new ImageGenerationRequest
                    {
                        Prompt = character.Prompt,
                        NegativePrompt = character.NegativePrompt,
                        Model = SelectedModel,
                        Lora = SelectedLora == "None" ? null : SelectedLora,
                        LoraWeight = LoraWeight,
                        Steps = Steps,
                        CfgScale = CfgScale,
                        Width = width,
                        Height = height,
                        SamplerName = SelectedSampler,
                        CharName = character.Name,
                        JsonFileName = SelectedJsonFile?.FileName,
                        JsonFileId = SelectedJsonFile?.Id,
                        BatchCount = 1
                    };

                    var result = await _sdService.GenerateAsync(request, _cts.Token);

                    if (result.Success)
                    {
                        foreach (var metadata in result.GeneratedImages)
                        {
                            await _dbService.SaveImageMetadataAsync(metadata);
                        }
                    }

                    current++;
                    Progress = (double)current / total * 100;

                    // Small delay between generations
                    await Task.Delay(500, _cts.Token);
                }
            }

            ProgressMessage = "Batch generation complete!";
        }
        catch (OperationCanceledException)
        {
            ProgressMessage = "Generation cancelled";
        }
        catch (Exception ex)
        {
            ProgressMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private bool CanGenerate() => !IsGenerating && SdConnected && Characters.Any(c => c.IsSelected);

    [RelayCommand]
    private void CancelGeneration()
    {
        _cts?.Cancel();
    }

    private static (int width, int height) ParseResolution(string resolution)
    {
        var parts = resolution.Split('x');
        return (int.Parse(parts[0]), int.Parse(parts[1]));
    }
}
