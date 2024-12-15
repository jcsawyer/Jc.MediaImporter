using Jc.MediaImporter.ViewModels.Import;
using ReactiveUI;

namespace Jc.MediaImporter.ViewModels;

public class ImportViewModel : ViewModelBase
{
    public ImportViewModel()
    {
    }
    
    private ViewModelBase _currentPage = new StartImportViewModel();

    public ViewModelBase CurrentPage
    {
        get => _currentPage;
        set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }
}