using System.Globalization;

namespace MusicDownloader.Common;

public static class TrackParser
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

        start = parts[0].Trim();
        end = parts[1].Trim();

        return !string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end);
    }

    public static bool TryParseTempo(string tempo, out double multiplier)
    {
        multiplier = 1.0;

        if (string.IsNullOrWhiteSpace(tempo) || !double.TryParse(tempo, NumberStyles.Any, CultureInfo.InvariantCulture, out double percent))
        {
            return false;
        }

        multiplier = percent / 100.0;
        return true;
    }
}