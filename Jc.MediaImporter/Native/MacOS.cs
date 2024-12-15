using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using Avalonia;

namespace Jc.MediaImporter.Native;

[SupportedOSPlatform("macOS")]
internal class MacOS : OS.IBackend
{
    public void SetupApp(AppBuilder builder)
    {
        builder.With(new MacOSPlatformOptions()
        {
            DisableDefaultApplicationMenuItems = true,
        });
    }
    
    public void OpenInFileManager(string path, bool select)
    {
        if (Directory.Exists(path))
            Process.Start("open", $"\"{path}\"");
        else if (File.Exists(path))
            Process.Start("open", $"\"{path}\" -R");
    }
}