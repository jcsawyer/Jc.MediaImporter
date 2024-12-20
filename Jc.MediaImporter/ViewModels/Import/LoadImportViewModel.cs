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
using Jc.MediaImporter.Helpers;
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
        ConfigureImportCommand = ReactiveCommand.Create(ConfigureImport);
        Task.Run(() => LoadMedia(SettingsViewModel.Instance.DefaultSourceDirectory));
    }

    public ICommand StopImportCommand { get; }
    public ICommand ConfigureImportCommand { get; }

    private CancellationTokenSource? _cancellationTokenSource;

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

    private string _currentFile;

    public string CurrentFile
    {
        get => _currentFile;
        set => this.RaiseAndSetIfChanged(ref _currentFile, value);
    }

    private void StopImport()
    {
        Dispatcher.UIThread.Post(() =>
        {
            _cancellationTokenSource?.Cancel();
            Error = string.Empty;
            CurrentFile = string.Empty;
            _import.StopImportCommand.Execute(null);
        });
    }

    private void ConfigureImport()
    {
        Dispatcher.UIThread.Post(() => _import.ConfigureImportCommand.Execute((Media, ErrorMedia)));
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

        Files.TraverseTreeParallelForEach(sourceDirectory, (file) =>
        {
            CurrentFile = file;
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
            var hash = Files.GetChecksum(vm.Path);
            vm.IsDuplicate = ManageViewModel.Instance.FileIndex.Any(x =>
                x.Value.Hash == hash ||
                x.Value.Path.Equals(
                    Path.Combine(SettingsViewModel.Instance.DefaultPhotosDirectory,
                        $"{vm.Date.Year}-{vm.Date.Month:00}", vm.SortedName + vm.Extension),
                    StringComparison.OrdinalIgnoreCase) || x.Value.Path.Equals(
                    Path.Combine(SettingsViewModel.Instance.DefaultVideosDirectory,
                        $"{vm.Date.Year}-{vm.Date.Month:00}", vm.SortedName + vm.Extension),
                    StringComparison.OrdinalIgnoreCase));

            Task.Run(() => vm.LoadThumbnailAsync(cancellationToken), cancellationToken);
            result.Add(vm);
        });
        Media = new ObservableCollection<MediaFileViewModel>(result.ToList());
        ErrorMedia = new ObservableCollection<MediaFileErrorViewModel>(errors.ToList());
        IsLoading = false;
    }
}