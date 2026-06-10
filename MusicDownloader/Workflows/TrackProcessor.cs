using MusicDownloader.Commands;
using MusicDownloader.Common;
using MusicDownloader.Infrastructure;
using System.ComponentModel;

namespace MusicDownloader.Workflows;

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

        string finalFormat = SettingsManager.Current.AudioFormat;
        string outputFile = Path.Combine(_albumDir, PathUtils.SafeFileName(_track.Title) + $".{finalFormat}");

        if (File.Exists(outputFile))
        {
            return false;
        }

        string tempFileBase = Path.Combine(_albumDir, "temp");
        string finalTempOut = Path.Combine(_albumDir, "out." + finalFormat);

        try
        {
            // 1. Download Full Audio + Thumbnail.
            if (!await RunFullDownloadAsync(tempFileBase))
            {
                return true;
            }

            // 2. Find the downloaded audio file (e.g., temp.m4a).
            string? downloadedAudio = FindDownloadedFile(tempFileBase);
            if (downloadedAudio is null)
            {
                Log.Error("Download reported success, but no audio file was found.");
                return true;
            }

            // 3. Find the downloaded cover art (e.g., temp.webp, temp.jpg).
            string? downloadedCover = FindCoverFile(tempFileBase);
            if (downloadedCover is not null)
            {
                Log.Info($"Found cover art: {Path.GetFileName(downloadedCover)}");
            }

            // 4. Process Audio (Trimming, Loop, Tempo, Embedding Cover).
            if (!await ProcessAudioAsync(_track, downloadedAudio, downloadedCover, finalTempOut))
            {
                return true;
            }

            File.Move(finalTempOut, outputFile, true);
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

    private static string? FindDownloadedFile(string baseName)
    {
        string dir = Path.GetDirectoryName(baseName)!;
        string fileName = Path.GetFileName(baseName); // "temp"

        string[] candidates = Directory.GetFiles(dir, $"{fileName}.*");

        return candidates.FirstOrDefault(f =>
        {
            string ext = Path.GetExtension(f).ToLowerInvariant();
            // Exclude common non-audio files
            return ext is not ".webp" and not ".jpg" and not ".png" and not ".json" and not ".part" and not ".ytdl";
        });
    }

    private static string? FindCoverFile(string baseName)
    {
        string dir = Path.GetDirectoryName(baseName)!;
        string fileName = Path.GetFileName(baseName); // "temp"

        string[] candidates = Directory.GetFiles(dir, $"{fileName}.*");

        // Prefer highest quality/common image formats
        return candidates.FirstOrDefault(f => f.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
            ?? candidates.FirstOrDefault(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
            ?? candidates.FirstOrDefault(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            ?? candidates.FirstOrDefault(f => f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> RunFullDownloadAsync(string tempFileBase)
    {
        Log.Action($"Downloading: {_track.Title}");

        string command = new YtDlpCommandBuilder(_track, tempFileBase).Build();
        string ytDlpPath = ExecutableFinder.GetFullPath(SettingsManager.Current.YtDlpExe, SettingsManager.Current.YtDlpDir);

        try
        {
            int exitCode = await Task.Run(() => ProcessExecutor.Run(ytDlpPath, command));

            if (exitCode != 0)
            {
                Log.Error($"Download failed for {_track.Title}.");
                return false;
            }

            return true;
        }
        catch (Win32Exception)
        {
            Log.Error($"Could not find '{SettingsManager.Current.YtDlpExe}'.");
            return false;
        }
    }

    private async Task<bool> ProcessAudioAsync(Track track, string inputFile, string? coverFile, string outputFile)
    {
        Log.Action($"Processing: {track.Title}");

        // Pass the coverFile to the builder so it can be embedded as an attached picture.
        string command = new FfmpegCommandBuilder(track, inputFile, outputFile, coverFile).Build();
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

    private void CleanupTempFiles()
    {
        if (!Directory.Exists(_albumDir))
        {
            return;
        }

        // Clean up everything starting with "temp." or "out."
        IEnumerable<string> tempFiles = Directory.EnumerateFiles(_albumDir, "temp.*")
            .Concat(Directory.EnumerateFiles(_albumDir, "out.*"));

        foreach (string file in tempFiles)
        {
            try
            {
                File.Delete(file);
            }
            catch { }
        }
    }
}