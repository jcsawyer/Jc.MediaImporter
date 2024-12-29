using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Jc.MediaImporter.Core;
using ReactiveUI;

namespace Jc.MediaImporter.ViewModels.Import;

public class ImportingViewModel : ViewModelBase
{
    private readonly ImportViewModel _import;

    private readonly IReadOnlyList<MediaFileViewModel> _photos;
    public IReadOnlyList<MediaFileViewModel> Photos => _photos;

    private readonly IReadOnlyList<MediaFileViewModel> _videos;
    public IReadOnlyList<MediaFileViewModel> Videos => _videos;

    private bool _isSeedingDirectories;
    public bool IsSeedingDirectories
    {
        get => _isSeedingDirectories;
        set => this.RaiseAndSetIfChanged(ref _isSeedingDirectories, value);
    }
    
    private bool _isImporting;

    public bool IsImporting
    {
        get => _isImporting;
        set => this.RaiseAndSetIfChanged(ref _isImporting, value);
    }

    private MediaFileViewModel _currentFile;

    public MediaFileViewModel CurrentFile
    {
        get => _currentFile;
        set => this.RaiseAndSetIfChanged(ref _currentFile, value);
    }

    private int _totalFiles;

    public int TotalFiles
    {
        get => _totalFiles;
        set => this.RaiseAndSetIfChanged(ref _totalFiles, value);
    }

    private int _completedFiles = 0;

    public int CompletedFiles
    {
        get => _completedFiles;
        set => this.RaiseAndSetIfChanged(ref _completedFiles, value);
    }

    public ImportingViewModel(ImportViewModel import, List<MediaFileViewModel> photos, List<MediaFileViewModel> videos)
    {
        _import = import ?? throw new ArgumentNullException(nameof(import));
        _photos = photos;
        _videos = videos;

        _totalFiles = _photos.Count + _videos.Count;

        Task.Run(Import);
    }

    private void Import()
    {
        Dispatcher.UIThread.Post(() => IsSeedingDirectories = true);

        void seedDirectories(string root, IEnumerable<IGrouping<string, MediaFileViewModel>> groupedFiles)
        {
            foreach (var group in groupedFiles)
            {
                Directory.CreateDirectory(Path.Combine(root, group.Key));
            }
        }

        var groupedPhotos = _photos.GroupBy(x => $"{x.Date.Year}-{x.Date.Month:00}");
        seedDirectories(Path.Combine(SettingsViewModel.Instance.DefaultPhotosDirectory), groupedPhotos);
        var groupedVideos = _videos.GroupBy(x => $"{x.Date.Year}-{x.Date.Month:00}");
        seedDirectories(Path.Combine(SettingsViewModel.Instance.DefaultPhotosDirectory), groupedVideos);

        var groupedMedia = _photos.Concat(_videos).GroupBy(x => $"{x.Date.Year}-{x.Date.Month:00}");
        Dispatcher.UIThread.Post(() => IsSeedingDirectories = false);

        var totalItems = groupedMedia.Sum(x => x.Count());
        Dispatcher.UIThread.Post(() => IsImporting = true);

        foreach (var group in groupedMedia)
        {
            foreach (var file in group)
            {
                Dispatcher.UIThread.Post(() => CurrentFile = file);
                var destination = file.Type switch
                {
                    MediaType.Photo => SettingsViewModel.Instance.DefaultPhotosDirectory,
                    MediaType.Video => SettingsViewModel.Instance.DefaultVideosDirectory,
                    _ => string.Empty,
                };

                if (string.IsNullOrWhiteSpace(destination))
                {
                    continue;
                }
                
                try
                {
                    File.Move(file.Path, Path.Combine(destination, group.Key, $"{file.SortedName}{file.Extension}"));
                }
                catch (IOException eex) when (eex.Message == "Cannot create a file when that file already exists.")
                {
                    var duplicates = Directory.GetFiles(Path.Combine(destination, group.Key), $"{file.SortedName}*");
                    try
                    {
                        File.Move(file.Path,
                            Path.Combine(destination, group.Key,
                                $"{file.SortedName}_{duplicates.Count()}{file.Extension}"));
                    }
                    catch (IOException)
                    {
                    }
                }
                catch (IOException ex)
                {
                    Console.WriteLine(ex);
                    // TODO handle file existing already?
                }
                finally
                {
                    var dir = Path.GetDirectoryName(file.Path);
                    if (Directory.EnumerateFiles(dir!, "*.*", SearchOption.AllDirectories)
                        .All(x => x.EndsWith("Thumbs.db", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".DS_Store", StringComparison.OrdinalIgnoreCase)))
                    {
                        Directory.Delete(dir, true);
                    }
                }
                
                Dispatcher.UIThread.Post(() => CompletedFiles++);
            }
        }

        Dispatcher.UIThread.Post(() => IsImporting = false);
    }
}