using System;
using System.Windows.Input;
using Avalonia.Threading;
using ReactiveUI;

namespace Jc.MediaImporter.ViewModels.Import;

public class StartImportViewModel : ViewModelBase
{
    private readonly ImportViewModel _import;

    public ICommand StartImportCommand { get; }

    public StartImportViewModel(ImportViewModel import)
    {
        _import = import ?? throw new ArgumentNullException(nameof(import));

        StartImportCommand = ReactiveCommand.Create(StartImport);
    }

    private void StartImport()
    {
        Dispatcher.UIThread.Post(() => { _import.StartImportCommand.Execute(null); });
    }
}