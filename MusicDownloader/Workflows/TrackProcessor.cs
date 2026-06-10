using MusicDownloader.Commands;
using MusicDownloader.Common;
using MusicDownloader.Infrastructure;
using System.ComponentModel;

namespace MusicDownloader.Workflows;

internal class TrackProcessor
{
    private readonly Track _track;
    private readonly string _albumDir;
    private readonly int _index;
    private readonly int _total;

    public TrackProcessor(Track track, int index = 0, int total = 0)
    {
        _track = track;
        _albumDir = Path.Combine(SettingsManager.Current.BaseDataDir, PathUtils.SafeFileName(_track.Album));
        _index = index;
        _total = total;
    }

    public static string GetOutputFile(Track track)
    {
        string albumDir = Path.Combine(SettingsManager.Current.BaseDataDir, PathUtils.SafeFileName(track.Album));
        string finalFormat = SettingsManager.Current.AudioFormat;
        return Path.Combine(albumDir, PathUtils.SafeFileName(track.Title) + $".{finalFormat}");
    }

    public async Task<TrackProcessStatus> ProcessAsync()
    {
        _ = Directory.CreateDirectory(_albumDir);

        string outputFile = GetOutputFile(_track);

        if (File.Exists(outputFile))
        {
            return TrackProcessStatus.Skipped;
        }

        string tempFileBase = Path.Combine(_albumDir, "temp");
        string finalTempOut = Path.Combine(_albumDir, "out." + SettingsManager.Current.AudioFormat);

        try
        {
            if (!await RunFullDownloadAsync(tempFileBase))
            {
                return TrackProcessStatus.Failed;
            }

            string? downloadedAudio = FindDownloadedFile(tempFileBase);
            if (downloadedAudio is null)
            {
                Log.Error($"{GetLogPrefix()}Download reported success, but no audio file was found.");
                return TrackProcessStatus.Failed;
            }

            string? downloadedCover = FindCoverFile(tempFileBase);
            if (downloadedCover is not null)
            {
                Log.Info($"{GetLogPrefix()}Found cover art: {Path.GetFileName(downloadedCover)}");
            }

            if (!await ProcessAudioAsync(_track, downloadedAudio, downloadedCover, finalTempOut))
            {
                return TrackProcessStatus.Failed;
            }

            File.Move(finalTempOut, outputFile, true);
            Log.Success($"{GetLogPrefix()}Done: {_track.Title}");
            return TrackProcessStatus.Success;
        }
        catch (Exception ex)
        {
            Log.Error($"{GetLogPrefix()}Failed processing '{_track.Title}': {ex.Message}");
            return TrackProcessStatus.Failed;
        }
        finally
        {
            CleanupTempFiles();
            Console.WriteLine();
        }
    }

    private string GetLogPrefix()
    {
        return _total > 0 ? $"[{_index}/{_total}] " : string.Empty;
    }

    private static string? FindDownloadedFile(string baseName)
    {
        string dir = Path.GetDirectoryName(baseName)!;
        string fileName = Path.GetFileName(baseName);

        string[] candidates = Directory.GetFiles(dir, $"{fileName}.*");

        return candidates.FirstOrDefault(f =>
        {
            string ext = Path.GetExtension(f).ToLowerInvariant();
            return ext is not ".webp" and not ".jpg" and not ".png" and not ".json" and not ".part" and not ".ytdl";
        });
    }

    private static string? FindCoverFile(string baseName)
    {
        string dir = Path.GetDirectoryName(baseName)!;
        string fileName = Path.GetFileName(baseName);

        string[] candidates = Directory.GetFiles(dir, $"{fileName}.*");

        return candidates.FirstOrDefault(f => f.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
            ?? candidates.FirstOrDefault(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
            ?? candidates.FirstOrDefault(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            ?? candidates.FirstOrDefault(f => f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> RunFullDownloadAsync(string tempFileBase)
    {
        Log.Action($"{GetLogPrefix()}Downloading: {_track.Title}");

        ProcessArguments command = new YtDlpCommandBuilder(_track, tempFileBase).Build();
        string ytDlpPath = ExecutableFinder.GetFullPath(SettingsManager.Current.YtDlpExe, SettingsManager.Current.YtDlpDir);

        try
        {
            int exitCode = await Task.Run(() => ProcessExecutor.Run(ytDlpPath, command));

            if (exitCode != 0)
            {
                Log.Error($"{GetLogPrefix()}Download failed for {_track.Title}.");
                return false;
            }

            return true;
        }
        catch (Win32Exception)
        {
            Log.Error($"{GetLogPrefix()}Could not find '{SettingsManager.Current.YtDlpExe}'.");
            return false;
        }
    }

    private async Task<bool> ProcessAudioAsync(Track track, string inputFile, string? coverFile, string outputFile)
    {
        Log.Action($"{GetLogPrefix()}Processing: {track.Title}");

        ProcessArguments command = new FfmpegCommandBuilder(track, inputFile, outputFile, coverFile).Build();
        string ffmpegPath = ExecutableFinder.GetFullPath(SettingsManager.Current.FfmpegExe, SettingsManager.Current.FfmpegDir);

        try
        {
            int exitCode = await Task.Run(() => ProcessExecutor.Run(ffmpegPath, command));

            if (exitCode != 0)
            {
                Log.Error($"{GetLogPrefix()}ffmpeg processing failed for {track.Title}.");
                return false;
            }
        }
        catch (Win32Exception)
        {
            Log.Error($"{GetLogPrefix()}Could not find '{SettingsManager.Current.FfmpegExe}'.");
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