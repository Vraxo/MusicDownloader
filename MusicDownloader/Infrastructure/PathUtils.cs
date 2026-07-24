
using MusicDownloader.Core;

namespace MusicDownloader.Infrastructure;

internal static class PathUtils
{
    public static readonly string[] SupportedExtensions = [".opus", ".m4a", ".mp3", ".flac", ".ogg", ".wav", ".aac"];

    public static string SafeFileName(string name)
    {
        return string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
    }

    public static string GetCoverFileName(Track track)
    {
        if (string.IsNullOrWhiteSpace(track.Cover))
        {
            return $"{SafeFileName(track.Title)}.png";
        }

        string cleanLink = track.Cover.Replace("[[", "").Replace("]]", "").Replace("/", "\\");
        return Path.GetFileName(cleanLink);
    }
}