using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using Jc.MediaImporter.Core;
using ReactiveUI;

namespace Jc.MediaImporter.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private static readonly string PhotoTarget = "Photos";
    private static readonly string VideoTarget = Path.Combine("Home Videos", "Captured");

    public MainWindowViewModel()
    {
        LoadMediaCommand = ReactiveCommand.Create(LoadMedia);
        ImportMediaCommand = ReactiveCommand.Create(ImportMedia);
    }

    public ICommand LoadMediaCommand { get; }
    public ICommand ImportMediaCommand { get; }

    private bool _isLoading;

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    private bool _isImporting;

    public bool IsImporting
    {
        get => _isImporting;
        set => this.RaiseAndSetIfChanged(ref _isImporting, value);
    }

    private string _sourceDirectory;

    public string SourceDirectory
    {
        get => _sourceDirectory;
        set => this.RaiseAndSetIfChanged(ref _sourceDirectory, value);
    }

    private string _targetDirectory;

    public string TargetDirectory
    {
        get => _targetDirectory;
        set => this.RaiseAndSetIfChanged(ref _targetDirectory, value);
    }

    private int _importProgress;

    public int ImportProgress
    {
        get => _importProgress;
        set => this.RaiseAndSetIfChanged(ref _importProgress, value);
    }

    private int _importProgressTotal;

    public int ImportProgressTotal
    {
        get => _importProgressTotal;
        set => this.RaiseAndSetIfChanged(ref _importProgressTotal, value);
    }

    public ObservableCollection<MediaFileViewModel> MediaFiles { get; } = new ObservableCollection<MediaFileViewModel>();

    private void ImportMedia()
    {
        if (!MediaFiles.Any() || IsLoading)
        {
            return;
        }

        IsImporting = true;

        // Run on another thread so we're not blocking
        Task.Run(ImportMediaImpl);
    }

    private void ImportMediaImpl()
    {
        void seedDirectories(string root, IEnumerable<IGrouping<string, MediaFileViewModel>> groupedFiles)
        {
            foreach (var group in groupedFiles)
            {
                Directory.CreateDirectory(Path.Combine(root, group.Key));
            }
        }

        var groupedPhotos = MediaFiles.Where(f => f.Type == MediaType.Photo)
            .GroupBy(f => $"{f.Date.Year}-{f.Date.Month:00}");
        seedDirectories(Path.Combine(TargetDirectory, PhotoTarget), groupedPhotos);
        var groupedVideos = MediaFiles.Where(f => f.Type == MediaType.Video)
            .GroupBy(f => $"{f.Date.Year}-{f.Date.Month:00}");
        seedDirectories(Path.Combine(TargetDirectory, VideoTarget), groupedVideos);

        var grouped = MediaFiles.GroupBy(f => $"{f.Date.Year}-{f.Date.Month:00}").ToList();

        var totalItems = grouped.Sum(g => g.Count());
        ImportProgress = 0;
        var processedItems = 0;
        var progress = 0;

        void ItemProcessed()
        {
            processedItems++;
            progress = (int)Math.Ceiling((float)processedItems / totalItems * 100);
            //worker!.ReportProgress(progress);
            Dispatcher.UIThread.Post(() =>
            {
                ImportProgress = processedItems;
            });
        }

        // TODO we also need to group by media type
        foreach (var group in grouped)
        {
            foreach (var file in group)
            {
                var targetPath = file.Type switch
                {
                    MediaType.Photo => Path.Combine(TargetDirectory, PhotoTarget),
                    MediaType.Video => Path.Combine(TargetDirectory, VideoTarget),
                    _ => string.Empty
                };
                if (string.IsNullOrEmpty(targetPath))
                {
                    ItemProcessed();
                    continue;
                }

                /*if (file.Type == MediaType.Video && file.Duration < TimeSpan.FromSeconds(10))
                {
                    // Try to avoid moving live photo snippets
                    ItemProcessed();
                    continue;
                }*/

                try
                {
                    File.Move(file.Path, Path.Combine(targetPath, group.Key, $"{file.SortedName}{file.Extension}"));
                }
                catch (IOException eex) when (eex.Message == "Cannot create a file when that file already exists.")
                {
                    var duplicates = Directory.GetFiles(Path.Combine(targetPath, group.Key), $"{file.SortedName}*");
                    try
                    {
                        File.Move(file.Path,
                            Path.Combine(targetPath, group.Key,
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
                        .All(f => f.EndsWith("Thumbs.db", StringComparison.OrdinalIgnoreCase)))
                    {
                        Directory.Delete(dir!, true);
                    }

                    ItemProcessed();
                }
            }
        }

        Dispatcher.UIThread.Post(() =>
        {
            MediaFiles.Clear();
            IsImporting = false;
        });
    }

    private void LoadMedia()
    {
        if (string.IsNullOrWhiteSpace(SourceDirectory) || IsImporting)
        {
            return;
        }

        IsLoading = true;
        MediaFiles.Clear();

        // Run on another thread so we're not blocking
        Task.Run(LoadMediaImpl);
    }

    private void LoadMediaImpl()
    {
        var paths = new[] { SourceDirectory }.ToList();
        var totalItems = paths.Count;
        var processedItems = 0;
        var progress = 0;


        (string Path, IReadOnlyList<MetadataExtractor.Directory?> Directories)? GetMetaData(string path)
        {
            try
            {
                var directories = MetadataExtractor.ImageMetadataReader.ReadMetadata(path);
                return (path, directories);
            }
            catch
            {
                // Swallow exception for now
                Debug.WriteLine($"Could not read metadata for file {path}");
                return null;
                // TODO Create a list of failures and report in UI
            }
        }

        foreach (var drop in paths)
        {
            //var directories = ImageMetadataReader.ReadMetadata(file);

            var attributes = File.GetAttributes(drop);
            if (attributes.HasFlag(FileAttributes.Directory))
            {
                var directoryFiles = Directory.EnumerateFiles(drop, "*.*", SearchOption.AllDirectories);
                totalItems += directoryFiles.Count() - 1;
                foreach (var file in directoryFiles)
                {
                    processedItems++;
                    progress = (int)Math.Ceiling((float)processedItems / totalItems * 100);
                    //worker!.ReportProgress(progress, GetMetaData(file));
                    var item = GetMetaData(file);
                    if (item.HasValue && !string.IsNullOrWhiteSpace(item.Value.Path) &&
                        item.Value.Directories is not null)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            var vm = new MediaFileViewModel(new MediaFile(item.Value.Path, item.Value.Directories!));
                            _ = vm.LoadThumbnailAsync();
                            MediaFiles.Add(vm);
                        });
                    }
                }
            }
            else
            {
                processedItems++;
                progress = (int)Math.Ceiling((float)processedItems / totalItems * 100);
                //worker!.ReportProgress(progress, GetMetaData(drop));
                var item = GetMetaData(drop);
                if (item.HasValue && !string.IsNullOrWhiteSpace(item.Value.Path) && item.Value.Directories is not null)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        var vm = new MediaFileViewModel(new MediaFile(item.Value.Path, item.Value.Directories!));
                        _ = vm.LoadThumbnailAsync();
                        MediaFiles.Add(vm);
                    });
                }
            }
        }


        Dispatcher.UIThread.Post(() =>
        {
            IsLoading = false;
            ImportProgressTotal = totalItems;
        });
    }
}