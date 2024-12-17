using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using DynamicData.Binding;
using Jc.MediaImporter.Helpers;
using Jc.MediaImporter.Native;
using MessagePack;
using ReactiveUI;

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
    private ConcurrentDictionary<string, DirectoryData> folderIndex = new ConcurrentDictionary<string, DirectoryData>();
    private ConcurrentDictionary<string, FileData> fileIndex = new ConcurrentDictionary<string, FileData>();

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
                using var folderStream = File.OpenRead(Path.Combine(OS.DataDir, "indexes", path.Replace('/', '_'), "folders.index"));
                folderIndex = MessagePackSerializer.Deserialize<ConcurrentDictionary<string, DirectoryData>>(folderStream);
            }
            
            if (File.Exists(Path.Combine(OS.DataDir, "indexes", path.Replace('/', '_'), "files.index")))
            {
                using var fileStream = File.OpenRead(Path.Combine(OS.DataDir, "indexes", path.Replace('/', '_'), "files.index"));
                fileIndex = MessagePackSerializer.Deserialize<ConcurrentDictionary<string, FileData>>(fileStream);
            }
            
            var prePrepared = !folderIndex.IsEmpty || !fileIndex.IsEmpty;
            if (prePrepared)
            {
                IsPreparing = false;
                IsIndexing = true;
            }

            Files.TraverseTreeParallelForEach(path, f =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var childCount = Directory.EnumerateFileSystemEntries(f, "*.*", new EnumerationOptions { IgnoreInaccessible = true, ReturnSpecialDirectories = false, RecurseSubdirectories = false}).Count();
                
                LoadingState = $"Preparing to index {itemCount} items...";
                
                if (folderIndex.TryGetValue(f, out var dir))
                {
                    if (!folderIndex.TryUpdate(f, new DirectoryData { Path = f, ChildCount = childCount, IsIndexed = true }, dir))
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
                
                if (!folderIndex.TryAdd(f, new DirectoryData { Path = f, ChildCount = childCount, IsIndexed = true }))
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

                if (!fileIndex.TryAdd(f, new FileData { Path = f, Size = size }))
                {
                    // TODO : Need to do something here for files that already exist.
                    Console.WriteLine("Skipping file {0}", f);
                }

                LoadingState = $"Preparing to index {itemCount} items...";
            }, ref itemCount, ref itemCount);
            
            foreach (var deletedDir in folderIndex.Where(f => !f.Value.IsIndexed))
            {
                // Folder was deleted...
                Console.WriteLine($"Folder {deletedDir.Key} was deleted...");
                folderIndex.TryRemove(deletedDir.Key, out _);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Indexing was cancelled.");
        }
        finally
        {
            using var folderStream = File.OpenWrite(Path.Combine(OS.DataDir, "indexes", path.Replace('/', '_'), "folders.index"));
            MessagePackSerializer.Serialize(folderStream, folderIndex);
            
            using var fileStream = File.OpenWrite(Path.Combine(OS.DataDir, "indexes", path.Replace('/', '_'), "files.index"));
            MessagePackSerializer.Serialize(fileStream, fileIndex);
        }
        
        Console.WriteLine("Finished in {0}ms", sw.ElapsedMilliseconds);
        
        IsPreparing = false;
        LoadingState = string.Empty;
        
        IsIndexing = true;
        Index(path, itemCount, cancellationToken);
    }

    private void Index(string path, int total, CancellationToken cancellationToken)
    {
        TotalItems = total - folderIndex.Count + fileIndex.Count;
        
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
                if (fileIndex.TryGetValue(f, out var file) && file.Size == size)
                {
                    // Skip files that are already indexed and haven't changed size
                    fileIndex.TryUpdate(f, file with { IsIndexed = true }, file);
                    return;
                }
                LoadingState = $"Indexing {f}";

                var hash = Files.GetChecksum(f);
                fileIndex.AddOrUpdate(f, f => new FileData { Size = size, Hash = hash, IsIndexed = true, Path = f },
                    (_, d) => d with { Hash = hash, IsIndexed = true });

                IndexedItems = indexedItems;
            }, ref indexedItems, ref indexedItems);
            
            foreach (var deletedFile in fileIndex.Where(f => !f.Value.IsIndexed))
            {
                // File was deleted...
                Console.WriteLine($"File {deletedFile.Key} was deleted...");
                fileIndex.TryRemove(deletedFile.Key, out _);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Indexing was cancelled.");
        }
        finally
        {
            using var fileStream = File.OpenWrite(Path.Combine(OS.DataDir, "indexes", path.Replace('/', '_'), "files.index"));
            MessagePackSerializer.Serialize(fileStream, fileIndex);
        }
        
        Console.WriteLine("Finished in {0}ms", sw.ElapsedMilliseconds);
        IsIndexing = false;
    }
    
    [MessagePackObject]
    public record DirectoryData
    {
        [Key(0)]
        public string Path { get; init; }

        [Key(1)]
        public int ChildCount { get; init; }
        
        [IgnoreMember]
        public bool IsIndexed { get; set; }
    }

    [MessagePackObject]
    public record FileData
    {
        [Key(0)]
        public string Path { get; init; }

        [Key(1)]
        public long Size { get; init; }

        [Key(2)]
        public string Hash { get; init; }
        
        [IgnoreMember]
        public bool IsIndexed { get; set; }
    }
}