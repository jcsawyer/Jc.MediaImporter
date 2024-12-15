using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using FluentAvalonia.UI.Windowing;
using Jc.MediaImporter.Models;
using Jc.MediaImporter.ViewModels;

namespace Jc.MediaImporter.Views;

public partial class MainWindow : AppWindow
{
    public MainWindow()
    {
        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;
        
        var rects = new[]
        {
            new Rect(0, 0, 100, 32),
            new Rect(300, 0, 100, 32)
        };

        TitleBar.SetDragRectangles(rects);
        
        InitializeComponent();
    }

    /*private async void SourceButton_OnClick(object? sender, RoutedEventArgs e)
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
    }*/
    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var navigationItem = e.AddedItems.OfType<NavigationMenuItem>().FirstOrDefault();
                if (navigationItem is null)
                {
                    return;
                }

                vm.ActiveNavigationItem = navigationItem; 
                vm.NavigateCommand.Execute(navigationItem);
            });
        }
    }
}