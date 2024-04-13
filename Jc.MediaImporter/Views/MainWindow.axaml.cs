using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace Jc.MediaImporter.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void SourceButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var options = new FolderPickerOpenOptions() { AllowMultiple = false };
        var toplevel = TopLevel.GetTopLevel(this);
        var selected = await toplevel!.StorageProvider.OpenFolderPickerAsync(options);
        if (selected.Count == 1)
        {
            SourceDirectory.Text = selected[0].Path.LocalPath;
        }

        e.Handled = true;
        
    }

    private async void TargetButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var options = new FolderPickerOpenOptions() { AllowMultiple = false };
        var toplevel = TopLevel.GetTopLevel(this);
        var selected = await toplevel!.StorageProvider.OpenFolderPickerAsync(options);
        if (selected.Count == 1)
        {
            TargetDirectory.Text = selected[0].Path.LocalPath;
        }

        e.Handled = true;
    }
}