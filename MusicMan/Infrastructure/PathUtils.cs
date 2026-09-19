using MusicMan.Core;

namespace MusicMan.Infrastructure;

internal static class PathUtils
{
    public static readonly string[] SupportedExtensions =
    [
        ".opus",
        ".m4a",
        ".mp3",
        ".flac",
        ".ogg",
        ".wav",
        ".aac"
    ];

    public static string SafeFileName(string name)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        string stripped = string.Concat(name.Where(c => !invalidChars.Contains(c)));
        return stripped.Trim();
    }

    public static string GetCoverFileName(Track track)
    {
        if (string.IsNullOrWhiteSpace(track.Cover))
        {
            return $"{SafeFileName(track.Title)}.png";
        }

        string cleanLink = track.Cover.Replace("[[", "").Replace("]]", "").Replace("/", "\\");
        string rawFileName = Path.GetFileName(cleanLink);
        return SafeFileName(rawFileName);
    }
}