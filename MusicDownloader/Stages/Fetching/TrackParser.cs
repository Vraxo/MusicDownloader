namespace MusicDownloader.Common;

internal static class TrackParser
{
    public static bool TryParseRange(string range, out string start, out string end)
    {
        start = string.Empty;
        end = string.Empty;

        if (string.IsNullOrWhiteSpace(range))
        {
            return false;
        }

        string[] parts = range.Split('-');
        if (parts.Length != 2)
        {
            return false;
        }

        string tempStart = parts[0].Trim();
        string tempEnd = parts[1].Trim();

        if (string.IsNullOrEmpty(tempStart) || string.IsNullOrEmpty(tempEnd))
        {
            return false;
        }

        start = tempStart;
        end = tempEnd;
        return true;
    }
}