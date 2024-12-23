using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Jc.MediaImporter.ViewModels.Import;
using ReactiveUI;

namespace Jc.MediaImporter.ViewModels;

public class ImportViewModel : ViewModelBase
{
    private static ImportViewModel? _instance;
    public static ImportViewModel Instance => _instance ??= new ImportViewModel();
    
    public ImportViewModel()
    {
        _currentPage = new StartImportViewModel(this);
        StopImportCommand = ReactiveCommand.Create(StopImport);
        StartImportCommand = ReactiveCommand.Create(StartImport);
        ConfigureImportCommand = ReactiveCommand.Create<(ObservableCollection<MediaFileViewModel>, ObservableCollection<MediaFileErrorViewModel>)>(ConfigureImport);
        ImportCommand = ReactiveCommand.Create<(List<MediaFileViewModel>, List<MediaFileViewModel>)>(Import);
    }
    
    public ICommand StopImportCommand { get; }
    public ICommand StartImportCommand { get; }
    public ICommand ConfigureImportCommand { get; }
    public ICommand ImportCommand { get; }
    
    private ViewModelBase _currentPage;

    public ViewModelBase CurrentPage
    {
        get => _currentPage;
        set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }
    
    private void StopImport()
    {
        // TODO going to need to issue cancellations, etc. to child VMs
        CurrentPage = new StartImportViewModel(this);
    }
    
    private void StartImport()
    {
        CurrentPage = new LoadImportViewModel(this);
    }
    
    private void ConfigureImport((ObservableCollection<MediaFileViewModel> Media, ObservableCollection<MediaFileErrorViewModel> Errors) media)
    {
        CurrentPage = new ConfigureImportViewModel(this, media.Media, media.Errors);
    }

    private void Import((List<MediaFileViewModel> Photos, List<MediaFileViewModel> Videos) media)
    {
        // TODO set current page to importing view
    }
}