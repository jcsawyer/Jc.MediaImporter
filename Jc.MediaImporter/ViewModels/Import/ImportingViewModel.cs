using System;
using System.Collections.Generic;

namespace Jc.MediaImporter.ViewModels.Import;

public class ImportingViewModel : ViewModelBase
{
    private readonly ImportViewModel _import;

    private readonly IReadOnlyList<MediaFileViewModel> _photos;
    public IReadOnlyList<MediaFileViewModel> Photos => _photos;
    
    private readonly IReadOnlyList<MediaFileViewModel> _videos;
    public IReadOnlyList<MediaFileViewModel> Videos => _videos;
    
    public ImportingViewModel(ImportViewModel import, List<MediaFileViewModel> photos, List<MediaFileViewModel> videos)
    {
        _import = import ?? throw new ArgumentNullException(nameof(import));
        _photos = photos;
        _videos = videos;
    }
}