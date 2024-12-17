using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReactiveUI;

namespace Jc.MediaImporter.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private static bool _isLoading = true;
    
    private static SettingsViewModel? _instance;

    [JsonIgnore]
    public static SettingsViewModel Instance
    {
        get
        {
            if (_instance is not null)
            {
                return _instance;
            }

            _isLoading = true;
            _instance = Load();
            _isLoading = false;

            return _instance;
        }
    }

    private string _defaultSourceDirectory;
    public string DefaultSourceDirectory
    {
        get => _defaultSourceDirectory;
        set => this.RaiseAndSetIfChanged(ref _defaultSourceDirectory, value);
    }
    
    private string _defaultPhotosDirectory;
    public string DefaultPhotosDirectory
    {
        get => _defaultPhotosDirectory;
        set => this.RaiseAndSetIfChanged(ref _defaultPhotosDirectory, value);
    }
    
    private string _defaultVideosDirectory;
    public string DefaultVideosDirectory
    {
        get => _defaultVideosDirectory;
        set => this.RaiseAndSetIfChanged(ref _defaultVideosDirectory, value);
    }

    private static SettingsViewModel Load()
    {
        var path = Path.Combine(Native.OS.DataDir, "settings.json");
        if (!File.Exists(path))
            return new SettingsViewModel();

        try
        {
            return JsonSerializer.Deserialize(File.ReadAllText(path), JsonCodeGen.Default.SettingsViewModel) ?? new SettingsViewModel();
        }
        catch
        {
            return new SettingsViewModel();
        }
    }

    internal void Save()
    {
        if (_isLoading)
            return;

        var file = Path.Combine(Native.OS.DataDir, "settings.json");
        var data = JsonSerializer.Serialize(this, JsonCodeGen.Default.SettingsViewModel);
        File.WriteAllText(file, data);
    }
}