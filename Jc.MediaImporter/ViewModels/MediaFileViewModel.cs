using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Jc.MediaImporter.Core;
using ReactiveUI;

namespace Jc.MediaImporter.ViewModels;

public sealed class MediaFileViewModel : ViewModelBase
{
    private readonly MediaFile _mediaFile;

    public MediaFileViewModel(MediaFile mediaFile)
    {
        _mediaFile = mediaFile;
    }
    
    public MediaType Type => _mediaFile.Type;

    public string Path => _mediaFile.Path;
    
    public string Name => _mediaFile.Name;

    public string Extension => _mediaFile.Extension;
    
    public string SortedName => _mediaFile.SortedName;
    
    public DateTime Date => _mediaFile.Date;

    private Bitmap? _thumbnail;
    public Bitmap? Thumbnail

    {
        get => _thumbnail;
        private set => this.RaiseAndSetIfChanged(ref _thumbnail, value);
    }

    public async Task LoadThumbnailAsync()
    {
        if (Type is MediaType.Photo)
        {
            await using var stream = File.OpenRead(Path);
            Thumbnail = await Task.Run(() => Bitmap.DecodeToWidth(stream, 100));
        }
        else
        {
            Thumbnail = new Bitmap(AssetLoader.Open(new Uri("avares://Jc.MediaImporter/Assets/Placeholder Image.png")));
        }
    }
}