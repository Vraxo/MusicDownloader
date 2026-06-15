using MusicDownloader.Common;
using Spectre.Console;
using System.Globalization;

namespace MusicDownloader.Infrastructure;

internal static class AudioProber
{
    public static int GetSampleRate(string inputFile)
    {
        return RunProbe(inputFile, "stream=sample_rate", (output) =>
        {
            return int.TryParse(output, out int val)
                ? val
                : -1;
        });
    }

    public static double GetDuration(string inputFile)
    {
        return RunProbe(inputFile, "format=duration", (output) =>
        {
            return double.TryParse(output, NumberStyles.Any, CultureInfo.InvariantCulture, out double val)
                ? val
                : -1.0;
        });
    }

    public static Dictionary<string, string> GetMetadata(string inputFile)
    {
        string ffprobeExe = SettingsManager.Current.FfmpegExe.Replace("ffmpeg", "ffprobe");
        string ffprobePath = ExecutableFinder.GetFullPath(ffprobeExe, SettingsManager.Current.FfmpegDir);

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
                        string key = line[..eqIndex].Replace("TAG:", "", StringComparison.OrdinalIgnoreCase).Trim();
                        string value = line[(eqIndex + 1)..].Trim();
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

    private static string Clean(string? val)
    {
        if (val is null)
        {
            return string.Empty;
        }
        return val.Trim().Replace("\r", "").Replace("\n", "");
    }

    public static bool IsMetadataUpToDate(string filePath, Track track, out string? mismatchReason)
    {
        Dictionary<string, string> existing = GetMetadata(filePath);
        List<string> mismatches = [];

        if (!existing.TryGetValue("title", out string? title))
        {
            title = string.Empty;
        }
        if (!string.Equals(Clean(title), Clean(track.Title), StringComparison.Ordinal))
        {
            mismatches.Add($"[gray]    - Title: '[/][red]{Clean(title).EscapeMarkup()}[/][gray]' -> '[/][green]{Clean(track.Title).EscapeMarkup()}[/][gray]'[/]");
        }

        if (!existing.TryGetValue("artist", out string? artist))
        {
            artist = string.Empty;
        }
        if (!string.Equals(Clean(artist), Clean(track.Artist), StringComparison.Ordinal))
        {
            mismatches.Add($"[gray]    - Artist: '[/][red]{Clean(artist).EscapeMarkup()}[/][gray]' -> '[/][green]{Clean(track.Artist).EscapeMarkup()}[/][gray]'[/]");
        }

        if (!existing.TryGetValue("album", out string? album))
        {
            album = string.Empty;
        }
        if (!string.Equals(Clean(album), Clean(track.Album), StringComparison.Ordinal))
        {
            mismatches.Add($"[gray]    - Album: '[/][red]{Clean(album).EscapeMarkup()}[/][gray]' -> '[/][green]{Clean(track.Album).EscapeMarkup()}[/][gray]'[/]");
        }

        string expectedTrack = track.TrackNumber?.ToString() ?? string.Empty;
        existing.TryGetValue("track", out string? existingTrack);
        if (existingTrack is not null && existingTrack.Contains('/'))
        {
            existingTrack = existingTrack.Split('/')[0];
        }
        if (!string.Equals(Clean(existingTrack), Clean(expectedTrack), StringComparison.Ordinal))
        {
            mismatches.Add($"[gray]    - Track: '[/][red]{Clean(existingTrack).EscapeMarkup()}[/][gray]' -> '[/][green]{Clean(expectedTrack).EscapeMarkup()}[/][gray]'[/]");
        }

        string expectedDisc = track.DiscNumber?.ToString() ?? string.Empty;
        existing.TryGetValue("disc", out string? existingDisc);
        if (existingDisc is not null && existingDisc.Contains('/'))
        {
            existingDisc = existingDisc.Split('/')[0];
        }
        if (!string.Equals(Clean(existingDisc), Clean(expectedDisc), StringComparison.Ordinal))
        {
            mismatches.Add($"[gray]    - Disc: '[/][red]{Clean(existingDisc).EscapeMarkup()}[/][gray]' -> '[/][green]{Clean(expectedDisc).EscapeMarkup()}[/][gray]'[/]");
        }

        existing.TryGetValue("date", out string? existingDate);
        string expectedDate = track.Date ?? string.Empty;
        if (!string.Equals(Clean(existingDate), Clean(expectedDate), StringComparison.Ordinal))
        {
            mismatches.Add($"[gray]    - Date: '[/][red]{Clean(existingDate).EscapeMarkup()}[/][gray]' -> '[/][green]{Clean(expectedDate).EscapeMarkup()}[/][gray]'[/]");
        }

        string expectedGenre = track.Tags.Count > 0 ? string.Join(", ", track.Tags) : string.Empty;
        existing.TryGetValue("genre", out string? existingGenre);
        if (!string.Equals(Clean(existingGenre), Clean(expectedGenre), StringComparison.Ordinal))
        {
            mismatches.Add($"[gray]    - Genre: '[/][red]{Clean(existingGenre).EscapeMarkup()}[/][gray]' -> '[/][green]{Clean(expectedGenre).EscapeMarkup()}[/][gray]'[/]");
        }

        existing.TryGetValue("comment", out string? existingComment);
        if (!string.Equals(Clean(existingComment), Clean(track.Source), StringComparison.Ordinal))
        {
            mismatches.Add($"[gray]    - Comment/Source: '[/][red]{Clean(existingComment).EscapeMarkup()}[/][gray]' -> '[/][green]{Clean(track.Source).EscapeMarkup()}[/][gray]'[/]");
        }

        if (mismatches.Count > 0)
        {
            mismatchReason = string.Join(Environment.NewLine, mismatches);
            return false;
        }

        mismatchReason = null;
        return true;
    }

    private static T RunProbe<T>(string inputFile, string entries, Func<string, T> parser)
    {
        string ffprobeExe = SettingsManager.Current.FfmpegExe.Replace("ffmpeg", "ffprobe");
        string ffprobePath = ExecutableFinder.GetFullPath(ffprobeExe, SettingsManager.Current.FfmpegDir);

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
            return result.ExitCode == 0 ? parser(result.StandardOutput) : parser("");
        }
        catch
        {
            return parser("");
        }
    }
}