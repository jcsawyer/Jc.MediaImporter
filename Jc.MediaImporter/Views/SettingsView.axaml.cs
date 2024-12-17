using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Jc.MediaImporter.ViewModels;

namespace Jc.MediaImporter.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }
    
    private async void SelectDefaultSourceDirectory(object _, RoutedEventArgs e)
    {
        var options = new FolderPickerOpenOptions() { AllowMultiple = false };
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var selected = await topLevel!.StorageProvider.OpenFolderPickerAsync(options);
            if (selected.Count == 1)
            {
                SettingsViewModel.Instance.DefaultSourceDirectory = selected[0].Path.LocalPath;
            }
        }
        catch (Exception ex)
        {
            //App.RaiseException(string.Empty, $"Failed to select default clone directory: {ex.Message}");
        }

        e.Handled = true;
    }
    
    private async void SelectDefaultPhotosDirectory(object _, RoutedEventArgs e)
    {
        var options = new FolderPickerOpenOptions() { AllowMultiple = false };
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var selected = await topLevel!.StorageProvider.OpenFolderPickerAsync(options);
            if (selected.Count == 1)
            {
                SettingsViewModel.Instance.DefaultPhotosDirectory = selected[0].Path.LocalPath;
            }
        }
        catch (Exception ex)
        {
            //App.RaiseException(string.Empty, $"Failed to select default clone directory: {ex.Message}");
        }

        e.Handled = true;
    }
    
    private async void SelectDefaultVideosDirectory(object _, RoutedEventArgs e)
    {
        var options = new FolderPickerOpenOptions() { AllowMultiple = false };
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var selected = await topLevel!.StorageProvider.OpenFolderPickerAsync(options);
            if (selected.Count == 1)
            {
                SettingsViewModel.Instance.DefaultVideosDirectory = selected[0].Path.LocalPath;
            }
        }
        catch (Exception ex)
        {
            //App.RaiseException(string.Empty, $"Failed to select default clone directory: {ex.Message}");
        }

        e.Handled = true;
    }
}