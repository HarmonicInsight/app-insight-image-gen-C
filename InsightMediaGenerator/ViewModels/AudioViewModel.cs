using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InsightMediaGenerator.Models;
using InsightMediaGenerator.Services.Interfaces;
using Microsoft.Win32;

namespace InsightMediaGenerator.ViewModels;

public partial class AudioViewModel : ObservableObject
{
    private readonly IVoicevoxService _voicevoxService;
    private readonly IAudioPlayerService _audioPlayer;
    private readonly AppConfig _config;
    private byte[]? _lastGeneratedAudio;
    private string? _lastFilePath;

    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private Speaker? _selectedSpeaker;

    [ObservableProperty]
    private double _speed = 1.0;

    [ObservableProperty]
    private double _pitch = 0.0;

    [ObservableProperty]
    private double _intonation = 1.0;

    [ObservableProperty]
    private double _volume = 1.0;

    [ObservableProperty]
    private bool _saveFile = true;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private bool _voicevoxConnected;

    [ObservableProperty]
    private string _statusMessage = "Checking...";

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private bool _hasGeneratedAudio;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private string _progressMessage = string.Empty;

    public ObservableCollection<Speaker> Speakers { get; } = new();

    public AudioViewModel(IVoicevoxService voicevoxService, IAudioPlayerService audioPlayer, AppConfig config)
    {
        _voicevoxService = voicevoxService;
        _audioPlayer = audioPlayer;
        _config = config;

        _audioPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
    }

    private void OnPlaybackStateChanged(object? sender, PlaybackStateChangedEventArgs e)
    {
        IsPlaying = e.IsPlaying;
    }

    public async Task InitializeAsync()
    {
        await CheckStatusAsync();
        if (VoicevoxConnected)
        {
            await LoadSpeakersAsync();
        }
    }

    [RelayCommand]
    private async Task CheckStatusAsync()
    {
        StatusMessage = "Checking...";

        // Try auto-discover first if configured
        if (_config.Voicevox.AutoDiscover)
        {
            VoicevoxConnected = await _voicevoxService.DiscoverEngineAsync();
        }
        else
        {
            VoicevoxConnected = await _voicevoxService.CheckConnectionAsync();
        }

        StatusMessage = VoicevoxConnected ? "Connected" : "Disconnected";

        if (VoicevoxConnected && Speakers.Count == 0)
        {
            await LoadSpeakersAsync();
        }
    }

    private async Task LoadSpeakersAsync()
    {
        try
        {
            var speakers = await _voicevoxService.GetSpeakersAsync();
            Speakers.Clear();
            foreach (var speaker in speakers)
                Speakers.Add(speaker);

            // Select default speaker
            var defaultSpeaker = Speakers.FirstOrDefault(s => s.Id == _config.Defaults.SpeakerId);
            SelectedSpeaker = defaultSpeaker ?? Speakers.FirstOrDefault();
        }
        catch (Exception ex)
        {
            ProgressMessage = $"Failed to load speakers: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private async Task GenerateAsync()
    {
        if (string.IsNullOrWhiteSpace(Text) || SelectedSpeaker == null)
            return;

        IsGenerating = true;
        ProgressMessage = "Generating audio...";

        try
        {
            var result = await _voicevoxService.GenerateAudioAsync(new AudioGenerationParams
            {
                Text = Text,
                SpeakerId = SelectedSpeaker.Id,
                Speed = Speed,
                Pitch = Pitch,
                Intonation = Intonation,
                Volume = Volume,
                SaveFile = SaveFile,
                FileName = FileName
            });

            if (result.Success && result.AudioData != null)
            {
                _lastGeneratedAudio = result.AudioData;
                _lastFilePath = result.FilePath;
                _audioPlayer.LoadAudio(result.AudioData);
                HasGeneratedAudio = true;
                ProgressMessage = SaveFile && result.FilePath != null
                    ? $"Generated: {Path.GetFileName(result.FilePath)}"
                    : "Audio generated successfully";
            }
            else
            {
                ProgressMessage = $"Generation failed: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            ProgressMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private bool CanGenerate() => !IsGenerating && VoicevoxConnected && !string.IsNullOrWhiteSpace(Text);

    [RelayCommand(CanExecute = nameof(HasGeneratedAudio))]
    private void Play()
    {
        _audioPlayer.Play();
    }

    [RelayCommand]
    private void Pause()
    {
        _audioPlayer.Pause();
    }

    [RelayCommand]
    private void Stop()
    {
        _audioPlayer.Stop();
    }

    [RelayCommand(CanExecute = nameof(HasGeneratedAudio))]
    private void Download()
    {
        if (_lastGeneratedAudio == null) return;

        var dialog = new SaveFileDialog
        {
            Filter = "WAV files (*.wav)|*.wav",
            FileName = string.IsNullOrEmpty(FileName)
                ? $"audio_{DateTime.Now:yyyyMMddHHmmss}.wav"
                : FileName.EndsWith(".wav") ? FileName : $"{FileName}.wav"
        };

        if (dialog.ShowDialog() == true)
        {
            File.WriteAllBytes(dialog.FileName, _lastGeneratedAudio);
            ProgressMessage = $"Saved: {dialog.FileName}";
        }
    }
}
