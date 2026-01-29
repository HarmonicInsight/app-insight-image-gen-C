using CommunityToolkit.Mvvm.ComponentModel;

namespace InsightMediaGenerator.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public SimpleImageViewModel SimpleImage { get; }
    public BatchImageViewModel BatchImage { get; }
    public AudioViewModel Audio { get; }

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public MainViewModel(
        SimpleImageViewModel simpleImage,
        BatchImageViewModel batchImage,
        AudioViewModel audio)
    {
        SimpleImage = simpleImage;
        BatchImage = batchImage;
        Audio = audio;
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            SimpleImage.InitializeAsync(),
            BatchImage.InitializeAsync(),
            Audio.InitializeAsync()
        );
    }
}
