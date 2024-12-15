using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using Avalonia;

namespace Jc.MediaImporter.Native;

[SupportedOSPlatform("linux")]
internal class Linux : OS.IBackend
{
    public void SetupApp(AppBuilder builder)
    {
        builder.With(new X11PlatformOptions() { EnableIme = true });
    }
    
    public void OpenInFileManager(string path, bool select)
    {
        if (Directory.Exists(path))
        {
            Process.Start("xdg-open", $"\"{path}\"");
        }
        else
        {
            var dir = Path.GetDirectoryName(path);
            if (Directory.Exists(dir))
                Process.Start("xdg-open", $"\"{dir}\"");
        }
    }
}