namespace MusicDownloader.Common;

internal static class PathUtils
{
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