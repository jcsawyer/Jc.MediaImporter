using System;
using System.IO;
using Avalonia;

namespace Jc.MediaImporter.Native;

public static class OS
{
    public interface IBackend
    {
        long GetFileSize(string path);
        void OpenInFileManager(string path, bool select);
        void SetupApp(AppBuilder builder);
    }

    public static string DataDir { get; private set; } = string.Empty;

    static OS()
    {
        if (OperatingSystem.IsWindows())
        {
            _backend = new Windows();
        }
        else if (OperatingSystem.IsMacOS())
        {
            _backend = new MacOS();
        }
        else if (OperatingSystem.IsLinux())
        {
            _backend = new Linux();
        }
        else
        {
            throw new Exception("Platform unsupported!!!");
        }
    }

    public static void OpenInFileManager(string path, bool select = false)
    {
        _backend.OpenInFileManager(path, select);
    }
    
    public static void SetupApp(AppBuilder builder)
    {
        _backend.SetupApp(builder);
    }

    public static void SetupDataDir()
    {
        var osAppDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrEmpty(osAppDataDir))
            DataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".jc-media-importer");
        else
            DataDir = Path.Combine(osAppDataDir, "JcMediaImporter");

        if (!Directory.Exists(DataDir))
            Directory.CreateDirectory(DataDir);
    }

    public static long GetFileSize(string path)
    {
        return _backend.GetFileSize(path);
    }

    private static IBackend _backend = null;
}
