using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Avalonia;
using Avalonia.Controls;

namespace Jc.MediaImporter.Native;

[SupportedOSPlatform("windows")]
internal class Windows : OS.IBackend
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct RTL_OSVERSIONINFOEX
    {
        internal uint dwOSVersionInfoSize;
        internal uint dwMajorVersion;
        internal uint dwMinorVersion;
        internal uint dwBuildNumber;
        internal uint dwPlatformId;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        internal string szCSDVersion;
    }

    [DllImport("ntdll")]
    private static extern int RtlGetVersion(ref RTL_OSVERSIONINFOEX lpVersionInformation);

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern bool PathFindOnPath([In, Out] StringBuilder pszFile, [In] string[] ppszOtherDirs);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern IntPtr ILCreateFromPathW(string pszPath);

    [DllImport("shell32.dll", SetLastError = false)]
    private static extern void ILFree(IntPtr pidl);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, int cild, IntPtr apidl, int dwFlags);

    public void OpenInFileManager(string path, bool select)
    {
        string fullpath;
        if (File.Exists(path))
        {
            fullpath = new FileInfo(path).FullName;
            select = true;
        }
        else
        {
            fullpath = new DirectoryInfo(path!).FullName;
            fullpath += Path.DirectorySeparatorChar;
        }

        if (select)
        {
            OpenFolderAndSelectFile(fullpath);
        }
        else
        {
            Process.Start(new ProcessStartInfo(fullpath)
            {
                UseShellExecute = true,
                CreateNoWindow = true,
            });
        }
    }
    
    private void OpenFolderAndSelectFile(string folderPath)
    {
        var pidl = ILCreateFromPathW(folderPath);

        try
        {
            SHOpenFolderAndSelectItems(pidl, 0, 0, 0);
        }
        finally
        {
            ILFree(pidl);
        }
    }
    
    public void SetupApp(AppBuilder builder)
    {
        // Fix drop shadow issue on Windows 10
        RTL_OSVERSIONINFOEX v = new RTL_OSVERSIONINFOEX();
        v.dwOSVersionInfoSize = (uint)Marshal.SizeOf<RTL_OSVERSIONINFOEX>();
        if (RtlGetVersion(ref v) == 0 && (v.dwMajorVersion < 10 || v.dwBuildNumber < 22000))
        {
            Window.WindowStateProperty.Changed.AddClassHandler<Window>((w, _) => FixWindowFrameOnWin10(w));
            Control.LoadedEvent.AddClassHandler<Window>((w, _) => FixWindowFrameOnWin10(w));
        }
    }
    
    private void FixWindowFrameOnWin10(Window w)
    {
        if (w.WindowState == WindowState.Maximized || w.WindowState == WindowState.FullScreen)
            w.SystemDecorations = SystemDecorations.Full;
        else if (w.WindowState == WindowState.Normal)
            w.SystemDecorations = SystemDecorations.BorderOnly;
    }
}