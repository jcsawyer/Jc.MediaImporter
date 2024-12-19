using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using Jc.MediaImporter.Core;
using Jc.MediaImporter.Helpers;
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
        
        _mediaCache.Connect()
            .Filter(x => x.Type == MediaType.Video)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _videos)
            .Subscribe();

        //Task.Run(() => IdentifyDuplicates());
        //ManageViewModel.Instance.Media.CollectionChanged += (_, _) => Task.Run(() => IdentifyDuplicates());
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
    
    private readonly ReadOnlyObservableCollection<MediaFileViewModel> _videos;
    public ReadOnlyObservableCollection<MediaFileViewModel> Videos => _videos;

    /*private void IdentifyDuplicates()
    {
        foreach (var photo in Photos)
        {
            var hash = Files.GetChecksum(photo.Path);
            // A photo is deemed a duplicate if it has the same hash as another photo or if it has the same sorted name as another photo
            photo.IsDuplicate = ManageViewModel.Instance.FileIndex.Any(x => x.Value.Hash == hash || x.Value.Path.Equals(Path.Combine(Path.GetDirectoryName(photo.Path), photo.SortedName + photo.Extension), StringComparison.OrdinalIgnoreCase));
        }
    }

    private static string GetTargetPath(string path, string sortedName, string extension)
    {
        return ;
    }*/
}