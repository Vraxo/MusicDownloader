using MusicDownloader.Common;
using Spectre.Console;
using System.Globalization;

namespace MusicDownloader.Infrastructure;

internal static class AudioProber
{
    public static int GetSampleRate(string inputFile)
    {
        return RunProbe(inputFile, "stream=sample_rate", output =>
        {
            return int.TryParse(output, out int val)
                ? val
                : -1;
        });
    }

    public static double GetDuration(string inputFile)
    {
        return RunProbe(inputFile, "format=duration", output =>
        {
            return double.TryParse(output, NumberStyles.Any, CultureInfo.InvariantCulture, out double val)
            ? val
            : -1.0;
        });
    }

    public static Dictionary<string, string> GetMetadata(string inputFile)
    {
        string ffprobePath = ExecutableFinder.GetFfprobePath();
        string[] args = [
            "-v", "error",
            "-show_entries", "format_tags",
            "-of", "default=noprint_wrappers=1",
            inputFile
        ];

        Dictionary<string, string> tags = new(StringComparer.OrdinalIgnoreCase);

        try
        {
            ProcessResult result = ProcessExecutor.RunAndCapture(ffprobePath, args);
            if (result.ExitCode == 0)
            {
                string[] lines = result.StandardOutput.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    int eqIndex = line.IndexOf('=');
                    if (eqIndex > 0)
                    {
                        string key = line[..eqIndex].Trim();
                        string value = line[(eqIndex + 1)..].Trim();

                        if (key.StartsWith("TAG:", StringComparison.OrdinalIgnoreCase))
                        {
                            key = key["TAG:".Length..].Trim();
                        }

                        tags[key] = value;
                    }
                }
            }
        }
        catch
        {
        }

        return tags;
    }

    public static bool IsMetadataUpToDate(string filePath, Track track, out string? mismatchReason)
    {
        Dictionary<string, string> existing = GetMetadata(filePath);
        List<string> mismatches = [];

        CheckField(
            existing,
            "title",
            track.Title,
            "Title",
            mismatches);

        CheckField(
            existing,
            "artist",
            track.Artist,
            "Artist",
            mismatches);

        CheckField(
            existing,
            "album_artist",
            track.AlbumArtist ?? string.Empty,
            "AlbumArtist",
            mismatches);

        CheckField(
            existing,
            "composer",
            track.Composer ?? string.Empty,
            "Composer",
            mismatches);

        CheckField(
            existing,
            "album",
            track.Album,
            "Album",
            mismatches);

        CheckField(
            existing,
            "track",
            track.TrackNumber?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            "Track",
            mismatches,
            splitOnSlash: true);

        CheckField(
            existing,
            "disc",
            track.DiscNumber?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            "Disc",
            mismatches,
            splitOnSlash: true);

        CheckField(
            existing,
            "date",
            track.Date ?? string.Empty,
            "Date",
            mismatches);

        CheckField(
            existing,
            "genre",
            track.Tags.Count > 0 ? string.Join(", ", track.Tags) : string.Empty,
            "Genre",
            mismatches);

        CheckField(
            existing,
            "comment",
            track.Source,
            "Comment/Source",
            mismatches);

        if (mismatches.Count > 0)
        {
            mismatchReason = string.Join(Environment.NewLine, mismatches);
            return false;
        }

        mismatchReason = null;
        return true;
    }

    private static void CheckField(
        Dictionary<string, string> existing,
        string key,
        string expectedValue,
        string displayName,
        List<string> mismatches,
        bool splitOnSlash = false)
    {
        existing.TryGetValue(key, out string? actualValue);
        actualValue ??= string.Empty;

        if (splitOnSlash && actualValue.Contains('/'))
        {
            actualValue = actualValue.Split('/')[0];
        }

        string cleanActual = actualValue.Trim().Replace("\r", "").Replace("\n", "");
        string cleanExpected = expectedValue.Trim().Replace("\r", "").Replace("\n", "");

        if (string.Equals(cleanActual, cleanExpected, StringComparison.Ordinal))
        {
            return;
        }

        mismatches.Add($"[gray]    - {displayName}: '[/][red]{cleanActual.EscapeMarkup()}[/][gray]' -> '[/][green]{cleanExpected.EscapeMarkup()}[/][gray]'[/]");
    }

    private static T RunProbe<T>(string inputFile, string entries, Func<string, T> parser)
    {
        string ffprobePath = ExecutableFinder.GetFfprobePath();
        string[] args = [
            "-v", "error",
            "-select_streams", "a:0",
            "-show_entries", entries,
            "-of", "default=noprint_wrappers=1:nokey=1",
            inputFile
        ];

        try
        {
            ProcessResult result = ProcessExecutor.RunAndCapture(ffprobePath, args);
            return result.ExitCode == 0
                ? parser(result.StandardOutput)
                : parser(string.Empty);
        }
        catch
        {
            return parser(string.Empty);
        }
    }
}