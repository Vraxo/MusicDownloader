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

    // Path.GetInvalidFileNameChars() only returns the host OS's characters, so names
    // sanitized on Linux would still be invalid when the library is used on Windows.
    private static readonly char[] WindowsInvalidFileNameChars =
        ['\"', '<', '>', '|', ':', '*', '?', '\\', '/', '\0'];

    public static string SafeFileName(string name)
    {
        char[] invalidChars = [.. Path.GetInvalidFileNameChars(), .. WindowsInvalidFileNameChars];
        string stripped = string.Concat(name.Where(c => !invalidChars.Contains(c)));
        return stripped.Trim();
    }

    public static string GetCoverFileName(Track track)
    {
        if (string.IsNullOrWhiteSpace(track.Cover))
        {
            return $"{SafeFileName(track.Title)}.png";
        }

        string cleanLink = track.Cover.Replace("[[", "").Replace("]]", "");
        string rawFileName = cleanLink.Split('/', '\\')[^1];
        return SafeFileName(rawFileName);
    }
}