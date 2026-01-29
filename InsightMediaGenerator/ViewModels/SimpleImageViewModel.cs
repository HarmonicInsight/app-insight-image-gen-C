using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InsightMediaGenerator.Models;
using InsightMediaGenerator.Services.Interfaces;

namespace InsightMediaGenerator.ViewModels;

public partial class SimpleImageViewModel : ObservableObject
{
    private readonly IStableDiffusionService _sdService;
    private readonly IDatabaseService _dbService;
    private readonly AppConfig _config;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private string _prompt = string.Empty;

    [ObservableProperty]
    private string _negativePrompt = "low quality, bad anatomy, worst quality, low resolution";

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
    private int _batchSize = 1;

    [ObservableProperty]
    private int _setCount = 1;

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

    public ObservableCollection<string> Models { get; } = new();
    public ObservableCollection<string> Loras { get; } = new();

    public ObservableCollection<string> Resolutions { get; } = new()
    {
        "512x512", "768x768", "1024x1024"
    };

    public ObservableCollection<string> Samplers { get; } = new()
    {
        "DPM++ 2M Karras", "Euler a", "Heun"
    };

    public ObservableCollection<int> BatchSizes { get; } = new() { 1, 2, 4 };
    public ObservableCollection<int> SetCounts { get; } = new() { 1, 2, 3, 5, 10 };

    public SimpleImageViewModel(IStableDiffusionService sdService, IDatabaseService dbService, AppConfig config)
    {
        _sdService = sdService;
        _dbService = dbService;
        _config = config;

        // Apply defaults from config
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

    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private async Task GenerateAsync()
    {
        if (string.IsNullOrWhiteSpace(Prompt) || string.IsNullOrEmpty(SelectedModel))
            return;

        IsGenerating = true;
        _cts = new CancellationTokenSource();
        Progress = 0;

        try
        {
            var (width, height) = ParseResolution(SelectedResolution);
            var totalSets = SetCount;
            var currentSet = 0;

            for (int set = 1; set <= SetCount; set++)
            {
                _cts.Token.ThrowIfCancellationRequested();

                ProgressMessage = $"Generating set {set}/{totalSets}...";

                var request = new ImageGenerationRequest
                {
                    Prompt = Prompt,
                    NegativePrompt = NegativePrompt,
                    Model = SelectedModel,
                    Lora = SelectedLora == "None" ? null : SelectedLora,
                    LoraWeight = LoraWeight,
                    Steps = Steps,
                    CfgScale = CfgScale,
                    Width = width,
                    Height = height,
                    SamplerName = SelectedSampler,
                    CharName = "simple",
                    BatchCount = BatchSize
                };

                var result = await _sdService.GenerateAsync(request, _cts.Token);

                if (result.Success)
                {
                    foreach (var metadata in result.GeneratedImages)
                    {
                        await _dbService.SaveImageMetadataAsync(metadata);
                    }
                }

                currentSet++;
                Progress = (double)currentSet / totalSets * 100;
            }

            ProgressMessage = "Generation complete!";
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

    private bool CanGenerate() => !IsGenerating && SdConnected && !string.IsNullOrWhiteSpace(Prompt);

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
