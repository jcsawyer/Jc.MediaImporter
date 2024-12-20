using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Jc.MediaImporter.Core;
using ReactiveUI;

namespace Jc.MediaImporter.ViewModels;

public sealed class MediaFileViewModel : ViewModelBase
{
    private static Bitmap DefaultThumbnail =
        new Bitmap(AssetLoader.Open(new Uri("avares://Jc.MediaImporter/Assets/Placeholder Image.png")));

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

    private Bitmap? _thumbnail = DefaultThumbnail;

    public Bitmap? Thumbnail
    {
        get => _thumbnail;
        private set => this.RaiseAndSetIfChanged(ref _thumbnail, value);
    }

    private bool _isDuplicate;

    public bool IsDuplicate
    {
        get => _isDuplicate;
        set => this.RaiseAndSetIfChanged(ref _isDuplicate, value);
    }

    public async Task LoadThumbnailAsync(CancellationToken cancellationToken)
    {
        if (Type is MediaType.Photo)
        {
            await using var stream = File.OpenRead(Path);
            Thumbnail = await Task.Run(() => Bitmap.DecodeToWidth(stream, 100), cancellationToken);
        }
        else if (Type is MediaType.Video)
        {
            var guid = Guid.NewGuid();
            var tempPath = System.IO.Path.Combine(Native.OS.DataDir, "temp", guid.ToString());
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await CreateThumbnailUsingFfmpeg(Path, tempPath);
                await using var stream = File.OpenRead(tempPath + ".jpg");
                Thumbnail = await Task.Run(() => Bitmap.DecodeToWidth(stream, 100), cancellationToken);
            }
            catch
            {
                Thumbnail = new Bitmap(
                    AssetLoader.Open(new Uri("avares://Jc.MediaImporter/Assets/Placeholder Image.png")));
            }
            finally
            {
                File.Delete(tempPath + ".jpg");
            }
        }
        else
        {
            Thumbnail = new Bitmap(AssetLoader.Open(new Uri("avares://Jc.MediaImporter/Assets/Placeholder Image.png")));
        }
    }

    static Task<long> CreateThumbnailUsingFfmpeg(string path, string tempPath)
    {
        var tcs = new TaskCompletionSource<long>();
        var process = new Process
        {
            StartInfo =
            {
                FileName = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                    "ffmpeg"),
                Arguments = "-ss 5 -an -i \"" + path + "\" -vframes:v 1 -update true -y \"" + tempPath + ".jpg\"",
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