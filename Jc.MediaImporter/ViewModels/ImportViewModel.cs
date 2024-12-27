using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Jc.MediaImporter.Core;
using Jc.MediaImporter.ViewModels.Import;
using ReactiveUI;

namespace Jc.MediaImporter.ViewModels;

public class ImportViewModel : ViewModelBase
{
    private static ImportViewModel? _instance;
    public static ImportViewModel Instance => _instance ??= new ImportViewModel();

    public ImportViewModel()
    {
        _currentPage = new StartImportViewModel(this);
        StopImportCommand = ReactiveCommand.Create(StopImport);
        StartImportCommand = ReactiveCommand.Create(StartImport);
        ConfigureImportCommand =
            ReactiveCommand
                .Create<(ObservableCollection<MediaFileViewModel>, ObservableCollection<MediaFileErrorViewModel>)>(
                    ConfigureImport);
        ImportCommand = ReactiveCommand.Create<(List<MediaFileViewModel>, List<MediaFileViewModel>)>(Import);
    }

    public ICommand StopImportCommand { get; }
    public ICommand StartImportCommand { get; }
    public ICommand ConfigureImportCommand { get; }
    public ICommand ImportCommand { get; }

    private ViewModelBase _currentPage;

    public ViewModelBase CurrentPage
    {
        get => _currentPage;
        set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }

    private void StopImport()
    {
        // TODO going to need to issue cancellations, etc. to child VMs
        CurrentPage = new StartImportViewModel(this);
    }

    private void StartImport()
    {
        CurrentPage = new LoadImportViewModel(this);
    }

    private void ConfigureImport(
        (ObservableCollection<MediaFileViewModel> Media, ObservableCollection<MediaFileErrorViewModel> Errors) media)
    {
        CurrentPage = new ConfigureImportViewModel(this, media.Media, media.Errors);
    }

    private void Import((List<MediaFileViewModel> Photos, List<MediaFileViewModel> Videos) media)
    {
        void seedDirectories(string root, IEnumerable<IGrouping<string, MediaFileViewModel>> groupedFiles)
        {
            foreach (var group in groupedFiles)
            {
                Directory.CreateDirectory(Path.Combine(root, group.Key));
            }
        }

        var groupedPhotos = media.Photos.GroupBy(x => $"{x.Date.Year}-{x.Date.Month:00}");
        seedDirectories(Path.Combine(SettingsViewModel.Instance.DefaultPhotosDirectory), groupedPhotos);
        var groupedVideos = media.Videos.GroupBy(x => $"{x.Date.Year}-{x.Date.Month:00}");
        seedDirectories(Path.Combine(SettingsViewModel.Instance.DefaultPhotosDirectory), groupedVideos);

        var groupedMedia = media.Photos.Concat(media.Videos).GroupBy(x => $"{x.Date.Year}-{x.Date.Month:00}");

        var totalItems = groupedMedia.Sum(x => x.Count());
        // TODO mark progress in an import view

        foreach (var group in groupedMedia)
        {
            foreach (var file in group)
            {
                var destination = file.Type switch
                {
                    MediaType.Photo => Path.Combine(SettingsViewModel.Instance.DefaultPhotosDirectory, group.Key,
                        file.Name),
                    MediaType.Video => Path.Combine(SettingsViewModel.Instance.DefaultVideosDirectory, group.Key,
                        file.Name),
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
                    // TODO hadnle file existing already?
                }
                finally
                {
                    var dir = Path.GetDirectoryName(file.Path);
                    if (Directory.EnumerateFiles(dir!, "*.*", SearchOption.AllDirectories)
                        .All(x => x.EndsWith("Thumbs.db", StringComparison.OrdinalIgnoreCase)))
                    {
                        Directory.Delete(dir, true);
                    }
                }
                // TODO mark progress in an import view
            }
        }

        // TODO set current page to importing view
    }
}