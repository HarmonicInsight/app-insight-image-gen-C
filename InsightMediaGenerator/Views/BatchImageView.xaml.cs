using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using InsightMediaGenerator.ViewModels;

namespace InsightMediaGenerator.Views;

public partial class BatchImageView : UserControl
{
    public BatchImageView()
    {
        InitializeComponent();
    }

    private void OnFileDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files?.Length > 0 && files[0].EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                if (DataContext is BatchImageViewModel vm)
                {
                    vm.UploadJsonFileCommand.Execute(files[0]);
                }
            }
        }
    }

    private void OnSelectJsonFile(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            Title = "Select JSON Prompt File"
        };

        if (dialog.ShowDialog() == true)
        {
            if (DataContext is BatchImageViewModel vm)
            {
                vm.UploadJsonFileCommand.Execute(dialog.FileName);
            }
        }
    }
}
