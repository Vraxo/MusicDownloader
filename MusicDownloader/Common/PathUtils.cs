namespace MusicDownloader.Common;

public static class PathUtils
{
    public static string SafeFileName(string name)
    {
        return string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
    }
}