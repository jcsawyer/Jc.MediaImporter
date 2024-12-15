using System;
using System.Windows.Input;
using Avalonia.Threading;
using ReactiveUI;

namespace Jc.MediaImporter.ViewModels.Import;

public class LoadImportViewModel : ViewModelBase
{
    private readonly ImportViewModel _import;
    
    public LoadImportViewModel(ImportViewModel import)
    {
        _import = import ?? throw new ArgumentNullException(nameof(import));
        StopImportCommand = ReactiveCommand.Create(StopImport);
    }
    
    public ICommand StopImportCommand { get; }
    
    private bool _isLoading = true;

    public bool IsLoading
    {
        get => _isLoading;
        set => _isLoading = value;
    }
    
    private void StopImport()
    {
        Dispatcher.UIThread.Post(() => { _import.StopImportCommand.Execute(null); });
    }
}