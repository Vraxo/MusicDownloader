namespace MusicDownloader.Common;

internal static class PathUtils
{
    public static string SafeFileName(string name)
    {
        return string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
    }
}