using System.Globalization;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.FileSystem;
using MetadataExtractor.Formats.QuickTime;

namespace Jc.MediaImporter.Core;

public sealed class MediaFile
{
    private static HashSet<string> _photoExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".png", ".jpeg", ".gif", ".nef", ".heic" };
    private static HashSet<string> _videoExtensions = new(StringComparer.OrdinalIgnoreCase) { ".avi", ".mov", ".mp4" };
    private static HashSet<string> _audioExtensions = new(StringComparer.OrdinalIgnoreCase) { ".mp3", ".flac", ".wma" };

    private readonly List<MetadataExtractor.Directory> _directories;
    public MediaFile(string path, IEnumerable<MetadataExtractor.Directory> directories)
    {
        Path = !string.IsNullOrEmpty(path) ? path : throw new ArgumentNullException(nameof(path));
        _directories = directories?.ToList() ?? throw new ArgumentNullException(nameof(directories));
    }

    public MediaType Type => _photoExtensions.Contains(Extension) 
        ? MediaType.Photo
        : _videoExtensions.Contains(Extension)
            ? MediaType.Video
            : MediaType.Other;
    
    public string Path { get; }

    public string Name => !string.IsNullOrEmpty(Path) ? System.IO.Path.GetFileNameWithoutExtension(Path) : string.Empty;

    public string Extension => !string.IsNullOrEmpty(Path) ? System.IO.Path.GetExtension(Path) : string.Empty;

    public string SortedName => !string.IsNullOrEmpty(Path)
        ? $"{Date.Year}{Date.Month:00}{Date.Day:00}_{Date.Hour:00}{Date.Minute:00}{Date.Second:00}"
        : string.Empty;
    
    public DateTime Date
    {
        get
        {
            DateTime fileDate;
            var hasFileDate = TryGetFileDate(out fileDate) && fileDate.Year > 1990;

            DateTime value;
            if (!TryGetExifDate(out value))
            {
                TryGetQuickTimeMovieDate(out value);
            }

            // Return file date when it's less than the metadata value but greater than a nonsense date
            return value == DateTime.MinValue || (hasFileDate && fileDate < value)
                ? fileDate
                : value;

        }
    }

    public TimeSpan? Duration
    {
        get
        {
            if (Type != MediaType.Video)
            {
                return null;
            }

            _ = TryGetQuickTimeMovieDuration(out var duration);
            return duration;
        }
    }

    public override int GetHashCode()
    {
        return Path.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is MediaFile mediaFile && mediaFile.Path.Equals(Path, StringComparison.OrdinalIgnoreCase);
    }

    private bool TryGetFileDate(out DateTime dateTime)
    {
        var dateTimeOriginal = _directories.OfType<FileMetadataDirectory>().FirstOrDefault()?.GetDescription(FileMetadataDirectory.TagFileModifiedDate);
        if (!string.IsNullOrEmpty(dateTimeOriginal))
        {
            if (dateTimeOriginal.Contains("Sept"))
            {
                dateTimeOriginal = dateTimeOriginal.Replace("Sept", "Sep");
            }
            dateTime = DateTime.ParseExact(dateTimeOriginal, "ddd MMM dd HH:mm:ss zzz yyyy", CultureInfo.InvariantCulture);
            return true;
        }
        dateTime = DateTime.MinValue;
        return false;
    }
    
    private bool TryGetExifDate(out DateTime dateTime)
    {
        var dateTimeOriginal = _directories.OfType<ExifSubIfdDirectory>().FirstOrDefault()?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);
        if (!string.IsNullOrWhiteSpace(dateTimeOriginal))
        {
            if (dateTimeOriginal.Contains("Sept"))
            {
                dateTimeOriginal = dateTimeOriginal.Replace("Sept", "Sep");
            }
            if (!DateTime.TryParse(dateTimeOriginal, out dateTime))
            {
                dateTime = DateTime.ParseExact(dateTimeOriginal, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture);
            }
            return true;
        }
        dateTime = DateTime.MinValue;
        return false;
    }

    private bool TryGetQuickTimeMovieDate(out DateTime dateTime)
    {
        var dateTimeOriginal = _directories.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault()?.GetDescription(QuickTimeMovieHeaderDirectory.TagCreated);
        if (!string.IsNullOrWhiteSpace(dateTimeOriginal))
        {
            if (dateTimeOriginal.Contains("Sept"))
            {
                dateTimeOriginal = dateTimeOriginal.Replace("Sept", "Sep");
            }
            dateTime = DateTime.ParseExact(dateTimeOriginal, "ddd MMM dd HH:mm:ss yyyy", CultureInfo.InvariantCulture);
            if (dateTime.Year < 2000)
            {
                dateTime = DateTime.MinValue;
                return false;
            }
            return true;
        }
        dateTime = DateTime.MinValue;
        return false;
    }

    private bool TryGetQuickTimeMovieDuration(out TimeSpan? duration)
    {
        var durationOriginal = _directories.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault()?.GetDescription(QuickTimeMovieHeaderDirectory.TagDuration);
        if (!string.IsNullOrWhiteSpace(durationOriginal))
        {
            duration = TimeSpan.Parse(durationOriginal, CultureInfo.InvariantCulture);
            return true;
        }
        duration = null;
        return false;
    }
}
