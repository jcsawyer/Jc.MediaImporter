using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
        else if (Type is MediaType.Video)
        {
            try
            {
                await CreateThumbnailUsingFfmpeg(Path);
                await using var stream = File.OpenRead(Path + ".jpg");
                Thumbnail = await Task.Run(() => Bitmap.DecodeToWidth(stream, 100));
            }
            catch
            {
                Thumbnail = new Bitmap(AssetLoader.Open(new Uri("avares://Jc.MediaImporter/Assets/Placeholder Image.png")));
            }
            finally
            {
                File.Delete(Path + ".jpg");
            }
        }
        else
        {
            Thumbnail = new Bitmap(AssetLoader.Open(new Uri("avares://Jc.MediaImporter/Assets/Placeholder Image.png")));
        }
    }

    static Task<long> CreateThumbnailUsingFfmpeg(string path)
    {
        var tcs = new TaskCompletionSource<long>();
        var process = new Process
        {
            StartInfo =
            {
                FileName = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "ffmpeg"),
                Arguments = "-ss 5 -an -i \"" + path + "\" -vframes:v 1 -update true -y \"" + path + ".jpg\"",
            },
            EnableRaisingEvents = true,
        };

        process.Exited += (sender, args) =>
        {
            process.Dispose();
            tcs.SetResult(0);
        };
        process.Start();
        return tcs.Task;
    }
}