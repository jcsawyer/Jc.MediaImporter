using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Jc.MediaImporter.ViewModels;
using Jc.MediaImporter.Views;
using ReactiveUI;

namespace Jc.MediaImporter;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        var settings = SettingsViewModel.Instance;
        settings.PropertyChanged += (_, _) => settings.Save();
        
        // Just instantiate the view model to make sure it's loaded
        _ = ManageViewModel.Instance;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
    
    public static void Quit(int exitCode)
    {
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow?.Close();
            desktop.Shutdown(exitCode);
        }
        else
        {
            Environment.Exit(exitCode);
        }
    }
}