using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using Jc.MediaImporter.Core;
using MetadataExtractor;
using ReactiveUI;
using Directory = System.IO.Directory;

namespace Jc.MediaImporter.ViewModels.Import;

public class LoadImportViewModel : ViewModelBase
{
    private readonly ImportViewModel _import;
    
    public LoadImportViewModel(ImportViewModel import)
    {
        _import = import ?? throw new ArgumentNullException(nameof(import));
        StopImportCommand = ReactiveCommand.Create(StopImport);
        Task.Run(() => LoadMedia(SettingsViewModel.Instance.DefaultSourceDirectory));
    }
    
    public ICommand StopImportCommand { get; }

    private CancellationTokenSource? _cancellationTokenSource;
    
    private ObservableCollection<MediaFileViewModel>? _media;

    public ObservableCollection<MediaFileViewModel>? Media
    {
        get => _media;
        set => this.RaiseAndSetIfChanged(ref _media, value);
    }
    
    private bool _isLoading = true;

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    private string _error;

    public string Error
    {
        get => _error;
        set => this.RaiseAndSetIfChanged(ref _error, value);
    }
    
    private void StopImport()
    {
        Dispatcher.UIThread.Post(() =>
        {
            _cancellationTokenSource?.Cancel();
            _import.StopImportCommand.Execute(null);
        });
    }

    private void LoadMedia(string sourceDirectory)
    {
        Media = null;
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;
        
        if (!Directory.Exists(sourceDirectory))
        {
            IsLoading = false;
            Error = "Invalid source directory, please check your settings and try again.";
        }
        
        (string Path, IReadOnlyList<MetadataExtractor.Directory?> Directories)? GetMetaData(string path)
        {
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(path);
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

        var result = new ConcurrentBag<MediaFileViewModel>();
        TraverseTreeParallelForEach(sourceDirectory, (file) =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            
            var item = GetMetaData(file);
            if (item is null)
            {
                return;
            }
            
            var vm = new MediaFileViewModel(new MediaFile(item.Value.Path, item.Value.Directories));
            Task.Run(() => vm.LoadThumbnailAsync(cancellationToken), cancellationToken);
            result.Add(vm);
        });
        Media = new ObservableCollection<MediaFileViewModel>(result.ToList());
        IsLoading = false;
    }
    
    private static void TraverseTreeParallelForEach(string root, Action<string> action)
    {
        //Count of files traversed and timer for diagnostic output
        int fileCount = 0;
        var sw = Stopwatch.StartNew();

        // Determine whether to parallelize file processing on each folder based on processor count.
        int procCount = Environment.ProcessorCount;

        // Data structure to hold names of subfolders to be examined for files.
        Stack<string> dirs = new Stack<string>();

        if (!Directory.Exists(root))
        {
            throw new ArgumentException(
                "The given root directory doesn't exist.", nameof(root));
        }
        dirs.Push(root);

        while (dirs.Count > 0)
        {
            string currentDir = dirs.Pop();
            string[] subDirs = { };
            string[] files = { };

            try
            {
                subDirs = Directory.GetDirectories(currentDir);
            }
            // Thrown if we do not have discovery permission on the directory.
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
                continue;
            }
            // Thrown if another process has deleted the directory after we retrieved its name.
            catch (DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
                continue;
            }

            try
            {
                files = Directory.GetFiles(currentDir);
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
                continue;
            }
            catch (DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
                continue;
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
                continue;
            }

            // Execute in parallel if there are enough files in the directory.
            // Otherwise, execute sequentially.Files are opened and processed
            // synchronously but this could be modified to perform async I/O.
            try
            {
                if (files.Length < procCount)
                {
                    foreach (var file in files)
                    {
                        action(file);
                        fileCount++;
                    }
                }
                else
                {
                    Parallel.ForEach(files, () => 0,
                        (file, loopState, localCount) =>
                        {
                            action(file);
                            return (int)++localCount;
                        },
                        (c) =>
                        {
                            Interlocked.Add(ref fileCount, c);
                        });
                }
            }
            catch (AggregateException ae)
            {
                ae.Handle((ex) =>
                {
                    if (ex is UnauthorizedAccessException)
                    {
                        // Here we just output a message and go on.
                        Console.WriteLine(ex.Message);
                        return true;
                    }
                    // Handle other exceptions here if necessary...

                    return false;
                });
            }

            // Push the subdirectories onto the stack for traversal.
            // This could also be done before handing the files.
            foreach (string str in subDirs)
                dirs.Push(str);
        }

        // For diagnostic purposes.
        Console.WriteLine("Processed {0} files in {1} milliseconds", fileCount, sw.ElapsedMilliseconds);
    }
}