using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia.Threading;
using DynamicData;
using Jc.MediaImporter.Core;
using ReactiveUI;

namespace Jc.MediaImporter.ViewModels.Import;

public class ConfigureImportViewModel : ViewModelBase
{
    private readonly ImportViewModel _import;
    
    public ConfigureImportViewModel(ImportViewModel import, ObservableCollection<MediaFileViewModel> media, ObservableCollection<MediaFileErrorViewModel> errorMedia)
    {
        _import = import ?? throw new ArgumentNullException(nameof(import));
        Media = media;
        ErrorMedia = errorMedia;

        ImportCommand = ReactiveCommand.Create(Import);

        foreach (var file in Media)
        {
            _mediaCache.AddOrUpdate(file);
        }
        _mediaCache.Connect()
            .Filter(x => x.Type == MediaType.Photo)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _photos)
            .Subscribe();
        
        _mediaCache.Connect()
            .Filter(x => x.Type == MediaType.Video)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _videos)
            .Subscribe();

        //Task.Run(() => IdentifyDuplicates());
        //ManageViewModel.Instance.Media.CollectionChanged += (_, _) => Task.Run(() => IdentifyDuplicates());
    }
    
    public ICommand ImportCommand { get; }
    
    private SourceCache<MediaFileViewModel, string> _mediaCache = new SourceCache<MediaFileViewModel, string>(x => x.Path);
    
    private ObservableCollection<MediaFileViewModel>? _media;
    public ObservableCollection<MediaFileViewModel>? Media
    {
        get => _media;
        set => this.RaiseAndSetIfChanged(ref _media, value);
    }

    private ObservableCollection<MediaFileErrorViewModel>? _errorMedia;
    public ObservableCollection<MediaFileErrorViewModel>? ErrorMedia
    {
        get => _errorMedia;
        set => this.RaiseAndSetIfChanged(ref _errorMedia, value);
    }
    
    private readonly ReadOnlyObservableCollection<MediaFileViewModel> _photos;
    public ReadOnlyObservableCollection<MediaFileViewModel> Photos => _photos;
    
    private readonly ReadOnlyObservableCollection<MediaFileViewModel> _videos;
    public ReadOnlyObservableCollection<MediaFileViewModel> Videos => _videos;

    private List<MediaFileViewModel> _selectedPhotos = new List<MediaFileViewModel>();
    public List<MediaFileViewModel> SelectedPhotos
    {
        get => _selectedPhotos;
        set => this.RaiseAndSetIfChanged(ref _selectedPhotos, value);
    }
    
    private List<MediaFileViewModel> _selectedVideos = new List<MediaFileViewModel>();
    public List<MediaFileViewModel> SelectedVideos
    {
        get => _selectedVideos;
        set => this.RaiseAndSetIfChanged(ref _selectedVideos, value);
    }
    
    private void Import()
    {
        Dispatcher.UIThread.Post(() => _import.ImportCommand.Execute((SelectedPhotos, SelectedVideos)));
    }
}