using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
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

        foreach (var file in Media)
        {
            _mediaCache.AddOrUpdate(file);
        }
        _mediaCache.Connect()
            .Filter(x => x.Type == MediaType.Photo)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _photos)
            .Subscribe();
    }
    
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
}