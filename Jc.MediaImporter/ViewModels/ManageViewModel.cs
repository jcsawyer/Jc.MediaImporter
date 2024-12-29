using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using Jc.MediaImporter.Core;
using Jc.MediaImporter.Helpers;
using Jc.MediaImporter.Models;
using Jc.MediaImporter.Native;
using MessagePack;
using MetadataExtractor;
using ReactiveUI;
using Directory = System.IO.Directory;

namespace Jc.MediaImporter.ViewModels;

public class ManageViewModel : ViewModelBase
{
    private static ManageViewModel? _instance;
    public static ManageViewModel Instance => _instance ??= new ManageViewModel();

    private ManageViewModel()
    {
        var settings = SettingsViewModel.Instance;
        settings.WhenPropertyChanged<SettingsViewModel, string>(x => x.DefaultPhotosDirectory)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .Subscribe(new Action<object>(_ => Task.Run(() => Prepare(settings.DefaultPhotosDirectory))));
    }

    private CancellationTokenSource _cancellationTokenSource;
    public ConcurrentDictionary<string, DirectoryData> FolderIndex = new ConcurrentDictionary<string, DirectoryData>();
    public ConcurrentDictionary<string, FileData> FileIndex = new ConcurrentDictionary<string, FileData>();

    private readonly ReadOnlyObservableCollection<MediaFileViewModel> _photos;
    public ReadOnlyObservableCollection<MediaFileViewModel> Photos => _photos;

    private bool _isPreparing = true;

    public bool IsPreparing
    {
        get => _isPreparing;
        set => this.RaiseAndSetIfChanged(ref _isPreparing, value);
    }

    private bool _isIndexing = false;

    public bool IsIndexing
    {
        get => _isIndexing;
        set => this.RaiseAndSetIfChanged(ref _isIndexing, value);
    }

    private bool _isLoading = false;

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    private string _loadingState;

    public string LoadingState
    {
        get => _loadingState;
        set => this.RaiseAndSetIfChanged(ref _loadingState, value);
    }

    private int _totalItems;

    public int TotalItems
    {
        get => _totalItems;
        set => this.RaiseAndSetIfChanged(ref _totalItems, value);
    }

    private int _indexedItems;

    public int IndexedItems
    {
        get => _indexedItems;
        set => this.RaiseAndSetIfChanged(ref _indexedItems, value);
    }

    private SourceCache<MediaFileViewModel, string> _mediaCache =
        new SourceCache<MediaFileViewModel, string>(x => x.Path);

    private ObservableCollection<MediaFileViewModel>? _media;

    public ObservableCollection<MediaFileViewModel>? Media
    {
        get => _media;
        set => this.RaiseAndSetIfChanged(ref _media, value);
    }

    private SortedObservableCollection<MediaFileViewModel>? _duplicates;

    public SortedObservableCollection<MediaFileViewModel>? Duplicates
    {
        get => _duplicates;
        set => this.RaiseAndSetIfChanged(ref _duplicates, value);
    }

    private void Prepare(string path)
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        if (!Directory.Exists(path))
        {
            // TODO show some kind of error
            return;
        }

        IsPreparing = true;
        var itemCount = 0;
        LoadingState = $"Preparing to index {itemCount} items...";


        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            Directory.CreateDirectory(Path.Combine(Native.OS.DataDir, "indexes", path.Replace('/', '_')));
            if (File.Exists(Path.Combine(OS.DataDir, "indexes", path.Replace('/', '_'), "folders.index")))
            {
                using var folderStream =
                    File.OpenRead(Path.Combine(OS.DataDir, "indexes", path.Replace('/', '_'), "folders.index"));
                FolderIndex =
                    MessagePackSerializer.Deserialize<ConcurrentDictionary<string, DirectoryData>>(folderStream);
            }

            if (File.Exists(Path.Combine(OS.DataDir, "indexes", path.Replace('/', '_'), "files.index")))
            {
                using var fileStream =
                    File.OpenRead(Path.Combine(OS.DataDir, "indexes", path.Replace('/', '_'), "files.index"));
                FileIndex = MessagePackSerializer.Deserialize<ConcurrentDictionary<string, FileData>>(fileStream);
            }

            var prePrepared = !FolderIndex.IsEmpty || !FileIndex.IsEmpty;

            Files.TraverseTreeParallelForEach(path, f =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var childCount = Directory.EnumerateFileSystemEntries(f, "*.*",
                        new EnumerationOptions
                        {
                            IgnoreInaccessible = true, ReturnSpecialDirectories = false, RecurseSubdirectories = false
                        })
                    .Count();

                LoadingState = $"Preparing to index {itemCount} items...";

                if (FolderIndex.TryGetValue(f, out var dir))
                {
                    if (!FolderIndex.TryUpdate(f,
                            new DirectoryData { Path = f, ChildCount = childCount, IsIndexed = true }, dir))
                    {
                        throw new Exception("Failed to update folder index");
                    }

                    if (dir.ChildCount == childCount)
                    {
                        // It's fair to assume we can skip the files of a directory with the same number of files...
                        Console.WriteLine("Skipping files for {0}", f);
                        return (true, childCount);
                    }

                    // Don't skip the files if the number of files is different
                    Console.WriteLine("Processing files for {0}", f);
                    return (false, childCount);
                }

                if (!FolderIndex.TryAdd(f, new DirectoryData { Path = f, ChildCount = childCount, IsIndexed = true }))
                {
                    throw new Exception("Failed to add to folder index");
                }

                // Don't skip the files if the directory doesn't exist in index 
                Console.WriteLine("Processing files for {0}", f);
                return (false, childCount);
            }, f =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var size = OS.GetFileSize(f);

                if (!FileIndex.TryAdd(f, new FileData { Path = f, Size = size }))
                {
                    // TODO : Need to do something here for files that already exist.
                    Console.WriteLine("Skipping file {0}", f);
                }

                LoadingState = $"Preparing to index {itemCount} items...";
            }, ref itemCount, ref itemCount);

            foreach (var deletedDir in FolderIndex.Where(f => !f.Value.IsIndexed))
            {
                // Folder was deleted...
                Console.WriteLine($"Folder {deletedDir.Key} was deleted...");
                FolderIndex.TryRemove(deletedDir.Key, out _);
            }

            if (prePrepared)
            {
                IsPreparing = false;
                IsIndexing = true;
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Indexing was cancelled.");
        }
        finally
        {
            using var folderStream =
                File.OpenWrite(Path.Combine(OS.DataDir, "indexes", path.Replace('/', '_'), "folders.index"));
            MessagePackSerializer.Serialize(folderStream, FolderIndex);

            using var fileStream =
                File.OpenWrite(Path.Combine(OS.DataDir, "indexes", path.Replace('/', '_'), "files.index"));
            MessagePackSerializer.Serialize(fileStream, FileIndex);
        }

        Console.WriteLine("Finished in {0}ms", sw.ElapsedMilliseconds);

        IsPreparing = false;
        LoadingState = string.Empty;

        IsIndexing = true;
        Index(path, itemCount, cancellationToken);
    }

    private void Index(string path, int total, CancellationToken cancellationToken)
    {
        TotalItems = total - FolderIndex.Count + FileIndex.Count;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var indexedItems = 0;
        try
        {
            Files.TraverseTreeParallelForEach(path, f =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                LoadingState = $"Indexing {f}";
                return (false, 0);
            }, f =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var size = OS.GetFileSize(f);
                if (FileIndex.TryGetValue(f, out var file) && file.Size == size && !string.IsNullOrEmpty(file.Hash))
                {
                    // Skip files that are already indexed and haven't changed size
                    FileIndex.TryUpdate(f, file with { IsIndexed = true }, file);
                    return;
                }

                LoadingState = $"Indexing {f}";

                var hash = Files.GetChecksum(f);
                FileIndex.AddOrUpdate(f, f => new FileData { Size = size, Hash = hash, IsIndexed = true, Path = f },
                    (_, d) => d with { Hash = hash, IsIndexed = true });

                IndexedItems = indexedItems;
            }, ref indexedItems, ref indexedItems);

            foreach (var deletedFile in FileIndex.Where(f => !f.Value.IsIndexed))
            {
                // File was deleted...
                Console.WriteLine($"File {deletedFile.Key} was deleted...");
                FileIndex.TryRemove(deletedFile.Key, out _);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Indexing was cancelled.");
        }
        finally
        {
            using var fileStream =
                File.OpenWrite(Path.Combine(OS.DataDir, "indexes", path.Replace('/', '_'), "files.index"));
            MessagePackSerializer.Serialize(fileStream, FileIndex);
        }

        Console.WriteLine("Finished in {0}ms", sw.ElapsedMilliseconds);
        IsIndexing = false;

        Load(cancellationToken);
    }

    private void Load(CancellationToken cancellationToken)
    {
        IsLoading = true;
        var result = new ConcurrentBag<MediaFileViewModel>();
        var errors = new ConcurrentBag<MediaFileErrorViewModel>();

        (string Path, IReadOnlyList<MetadataExtractor.Directory?> Directories)? GetMetaData(string path)
        {
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(path);
                return (path, directories);
            }
            catch (Exception ex)
            {
                if (path.Contains(".DS_Store", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Equals("File format could not be determined", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                errors.Add(new MediaFileErrorViewModel { Path = path, Error = ex.Message });
                return null;
            }
        }

        Files.TraverseTreeParallelForEach(FileIndex.Keys.ToList(), file =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            LoadingState = $"Loading {file}";

            var item = GetMetaData(file);
            if (item is null)
            {
                return;
            }

            var vm = new MediaFileViewModel(new MediaFile(item.Value.Path, item.Value.Directories));
            Task.Run(() => vm.LoadThumbnailAsync(cancellationToken), cancellationToken);
            result.Add(vm);
        });

        LoadingState = string.Empty;
        IsLoading = false;

        Media = new ObservableCollection<MediaFileViewModel>(result.ToList());

        var dupes = FileIndex.GroupBy(x => x.Value.Hash).Where(x => x.Count() > 1).SelectMany(x => x)
            .ToDictionary();
        Duplicates = new SortedObservableCollection<MediaFileViewModel>(new List<MediaFileViewModel>())
        {
            SortingSelector = x => x.SortedName,
            Descending = true,
        };
        Duplicates.AddRange(Media.Where(x => dupes.ContainsKey(x.Path)));
    }

    [MessagePackObject]
    public record DirectoryData
    {
        [Key(0)] public string Path { get; init; }

        [Key(1)] public int ChildCount { get; init; }

        [IgnoreMember] public bool IsIndexed { get; set; }
    }

    [MessagePackObject]
    public record FileData
    {
        [Key(0)] public string Path { get; init; }

        [Key(1)] public long Size { get; init; }

        [Key(2)] public string Hash { get; init; }

        [IgnoreMember] public bool IsIndexed { get; set; }
    }
}