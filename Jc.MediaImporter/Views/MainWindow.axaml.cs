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
        
        InitializeComponent();
    }

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