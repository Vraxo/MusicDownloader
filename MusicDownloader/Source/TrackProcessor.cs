using System.ComponentModel;
using System.Globalization;

namespace MusicDownloader;

public class TrackProcessor
{
    private readonly Track _track;
    private readonly string _albumDir;

    public TrackProcessor(Track track)
    {
        _track = track;
        _albumDir = Path.Combine(SettingsManager.Current.BaseDataDir, PathUtils.SafeFileName(_track.Album));
    }

    public async Task<bool> ProcessAsync()
    {
        _ = Directory.CreateDirectory(_albumDir);

        string format = SettingsManager.Current.AudioFormat;
        string outputFile = Path.Combine(_albumDir, PathUtils.SafeFileName(_track.Title) + $".{format}");

        if (File.Exists(outputFile))
        {
            return false;
        }

        string tempFile = Path.Combine(_albumDir, "temp." + format);
        string outFile = Path.Combine(_albumDir, "out." + format);

        try
        {
            // 1. Attempt efficient Partial Download.
            if (!await DownloadWithFallbackAsync(tempFile))
            {
                return true;
            }

            // 2. Determine if we need to trim locally.
            Track trackForProcessing = _track;

            if (!string.IsNullOrWhiteSpace(_track.Range))
            {
                double actualDuration = AudioProber.GetDuration(tempFile);
                double expectedDuration = ParseDuration(_track.Range);

                // If actual duration matches expected (tolerance 10s), the partial download worked.
                if (actualDuration > 0 && expectedDuration > 0 && Math.Abs(actualDuration - expectedDuration) < 10)
                {
                    Log.Info($"Partial download success ({actualDuration:F1}s). Skipping local trim.");
                    trackForProcessing = _track with { Range = string.Empty };
                }
                else
                {
                    Log.Info($"Full file detected ({actualDuration:F1}s). Local trim will be applied.");
                }
            }

            // 3. Process Audio (Convert, Loop, Tempo, Metadata).
            if (!await ProcessAudioAsync(trackForProcessing, tempFile, outFile))
            {
                return true;
            }

            File.Move(outFile, outputFile, true);
            Log.Success($"Done: {_track.Title}");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed processing '{_track.Title}': {ex.Message}");
        }
        finally
        {
            CleanupTempFiles();
            Console.WriteLine();
        }

        return true;
    }

    private async Task<bool> DownloadWithFallbackAsync(string tempFilePath)
    {
        // Strategy 1: Try Partial Download.
        // We pass 'isPartial: true' to enforce strict format rules.
        // We pass 'suppressErrors: true' to log yt-dlp errors as Warnings, not Errors.
        bool isPartial = !string.IsNullOrEmpty(_track.Range);
        if (await RunDownloadAsync(_track, tempFilePath, suppressErrors: isPartial, isPartial: isPartial))
        {
            return true;
        }

        // Strategy 2: Fallback to Full Download.
        if (isPartial)
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }

            Console.WriteLine();
            Log.Warning("Partial download strategy unavailable. Switching to Full Download Fallback...");
            Log.Action(">>> DOWNLOADING FULL FILE (Safe Mode)...");

            Track fullTrack = _track with { Range = string.Empty };

            // For the fallback, we DO want to see real errors in Red if it fails.
            if (await RunDownloadAsync(fullTrack, tempFilePath, suppressErrors: false, isPartial: false))
            {
                return true;
            }
        }

        Log.Error($"All download attempts failed for {_track.Title}.");
        return false;
    }

    private async Task<bool> RunDownloadAsync(Track track, string tempFilePath, bool suppressErrors, bool isPartial)
    {
        string mode = isPartial ? "Partial" : "Full";
        Log.Action($"Downloading ({mode}): {track.Title}");

        string command = new YtDlpCommandBuilder(track, tempFilePath, isPartial).Build();
        string ytDlpPath = ExecutableFinder.GetFullPath(SettingsManager.Current.YtDlpExe, SettingsManager.Current.YtDlpDir);

        // Custom handler: If we are suppressing errors, print everything from stderr as a Warning or Info.
        Action<string>? errorHandler = null;

        if (suppressErrors)
        {
            errorHandler = (data) =>
            {
                // Filter out non-critical progress info if needed, but mainly ensure it's not RED.
                if (data.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                    data.Contains("failed", StringComparison.OrdinalIgnoreCase))
                {
                    Log.Warning($"[Speculative] {data}");
                }
                else
                {
                    Log.Info(data);
                }
            };
        }

        try
        {
            int exitCode = await Task.Run(() => ProcessExecutor.Run(ytDlpPath, command, errorHandler));
            return exitCode == 0;
        }
        catch (Win32Exception)
        {
            Log.Error($"Could not find '{SettingsManager.Current.YtDlpExe}'.");
            return false;
        }
        catch (Exception ex)
        {
            if (suppressErrors)
            {
                Log.Warning($"Download attempt skipped ({ex.Message}). Retrying...");
            }
            else
            {
                Log.Error($"Download error for {track.Title}: {ex.Message}");
            }
            return false;
        }
    }

    private async Task<bool> ProcessAudioAsync(Track track, string inputFile, string outputFile)
    {
        Log.Action($"Processing: {track.Title}");

        string command = new FfmpegCommandBuilder(track, inputFile, outputFile).Build();
        string ffmpegPath = ExecutableFinder.GetFullPath(SettingsManager.Current.FfmpegExe, SettingsManager.Current.FfmpegDir);

        try
        {
            int exitCode = await Task.Run(() => ProcessExecutor.Run(ffmpegPath, command));

            if (exitCode != 0)
            {
                Log.Error($"ffmpeg processing failed for {track.Title}.");
                return false;
            }
        }
        catch (Win32Exception)
        {
            Log.Error($"Could not find '{SettingsManager.Current.FfmpegExe}'.");
            return false;
        }

        return true;
    }

    private double ParseDuration(string range)
    {
        try
        {
            string[] parts = range.Split('-');
            if (parts.Length != 2)
            {
                return -1;
            }

            TimeSpan start = ParseTime(parts[0]);
            TimeSpan end = ParseTime(parts[1]);
            return (end - start).TotalSeconds;
        }
        catch { return -1; }
    }

    private TimeSpan ParseTime(string timeStr)
    {
        return TimeSpan.TryParse(timeStr, CultureInfo.InvariantCulture, out TimeSpan ts)
            ? ts
            : double.TryParse(timeStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double sec)
            ? TimeSpan.FromSeconds(sec)
            : TimeSpan.Zero;
    }

    private void CleanupTempFiles()
    {
        IEnumerable<string> tempFiles = Directory.EnumerateFiles(_albumDir, "temp.*")
            .Concat(Directory.EnumerateFiles(_albumDir, "out.*"));

        foreach (string file in tempFiles)
        {
            try { File.Delete(file); } catch { }
        }
    }
}