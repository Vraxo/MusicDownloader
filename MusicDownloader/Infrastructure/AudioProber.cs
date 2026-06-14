using MusicDownloader.Common;
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
            // Fail silently, return empty tags
        }

        return tags;
    }

    public static bool IsMetadataUpToDate(string filePath, Track track)
    {
        Dictionary<string, string> existing = GetMetadata(filePath);

        if (!existing.TryGetValue("title", out string? title) || !string.Equals(title, track.Title, StringComparison.Ordinal))
        {
            return false;
        }

        if (!existing.TryGetValue("artist", out string? artist) || !string.Equals(artist, track.Artist, StringComparison.Ordinal))
        {
            return false;
        }

        if (!existing.TryGetValue("album", out string? album) || !string.Equals(album, track.Album, StringComparison.Ordinal))
        {
            return false;
        }

        string expectedTrack = track.TrackNumber?.ToString() ?? string.Empty;
        existing.TryGetValue("track", out string? existingTrack);
        if (existingTrack is not null && existingTrack.Contains('/'))
        {
            existingTrack = existingTrack.Split('/')[0];
        }
        if (!string.Equals(existingTrack ?? string.Empty, expectedTrack, StringComparison.Ordinal))
        {
            return false;
        }

        string expectedDisc = track.DiscNumber?.ToString() ?? string.Empty;
        existing.TryGetValue("disc", out string? existingDisc);
        if (existingDisc is not null && existingDisc.Contains('/'))
        {
            existingDisc = existingDisc.Split('/')[0];
        }
        if (!string.Equals(existingDisc ?? string.Empty, expectedDisc, StringComparison.Ordinal))
        {
            return false;
        }

        existing.TryGetValue("date", out string? existingDate);
        if (!string.Equals(existingDate ?? string.Empty, track.Date ?? string.Empty, StringComparison.Ordinal))
        {
            return false;
        }

        string expectedGenre = track.Tags.Count > 0 ? string.Join(", ", track.Tags) : string.Empty;
        existing.TryGetValue("genre", out string? existingGenre);
        if (!string.Equals(existingGenre ?? string.Empty, expectedGenre, StringComparison.Ordinal))
        {
            return false;
        }

        existing.TryGetValue("comment", out string? existingComment);
        if (!string.Equals(existingComment ?? string.Empty, track.Source, StringComparison.Ordinal))
        {
            return false;
        }

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