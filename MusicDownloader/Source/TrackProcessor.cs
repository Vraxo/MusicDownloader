using System.ComponentModel;

namespace MusicDownloader;

public class TrackProcessor
{
    private readonly Track _track;
    private readonly string _albumDir;

    public TrackProcessor(Track track)
    {
        _track = track;
        _albumDir = Path.Combine(AppSettings.BaseDataDir, PathUtils.SafeFileName(_track.Album));
    }

    public async Task ProcessAsync()
    {
        Directory.CreateDirectory(_albumDir);

        string outputFile = Path.Combine(_albumDir, PathUtils.SafeFileName(_track.Title) + $".{AppSettings.AudioFormat}");

        if (File.Exists(outputFile))
        {
            return;
        }

        string tempFile = Path.Combine(_albumDir, "temp." + AppSettings.AudioFormat);
        string outFile = Path.Combine(_albumDir, "out." + AppSettings.AudioFormat);

        try
        {
            if (!await DownloadAsync(tempFile))
            {
                return;
            }

            if (!await ProcessAudioAsync(tempFile, outFile))
            {
                return;
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
    }

    private async Task<bool> DownloadAsync(string tempFilePath)
    {
        Log.Action($"Downloading: {_track.Title}");

        string command = new YtDlpCommandBuilder(_track, tempFilePath).Build();

        string ytDlpPath = !string.IsNullOrWhiteSpace(AppSettings.YtDlpDir)
            ? Path.Combine(AppSettings.YtDlpDir, AppSettings.YtDlpExe)
            : AppSettings.YtDlpExe;

        try
        {
            int exitCode = await Task.Run(() => ProcessExecutor.Run(ytDlpPath, command));

            if (exitCode != 0)
            {
                Log.Error($"yt-dlp failed for {_track.Title}, skipping...");
                return false;
            }
        }
        catch (Win32Exception)
        {
            Log.Error($"Could not find '{ytDlpPath}'. Make sure yt-dlp is in your PATH or the YtDlpDir is set correctly in AppSettings.");
            return false;
        }

        return true;
    }

    private async Task<bool> ProcessAudioAsync(string inputFile, string outputFile)
    {
        Log.Action($"Processing: {_track.Title}");

        string command = new FfmpegCommandBuilder(_track, inputFile, outputFile).Build();

        string ffmpegPath = !string.IsNullOrWhiteSpace(AppSettings.FfmpegDir)
            ? Path.Combine(AppSettings.FfmpegDir, AppSettings.FfmpegExe)
            : AppSettings.FfmpegExe;

        try
        {
            int exitCode = await Task.Run(() => ProcessExecutor.Run(ffmpegPath, command));

            if (exitCode != 0)
            {
                Log.Error($"ffmpeg processing failed for {_track.Title}, skipping...");
                return false;
            }
        }
        catch (Win32Exception)
        {
            Log.Error($"Could not find '{ffmpegPath}'. Make sure ffmpeg is in your PATH or the FfmpegDir is set correctly in AppSettings.");
            return false;
        }

        return true;
    }

    private void CleanupTempFiles()
    {
        IEnumerable<string> tempFiles = Directory.EnumerateFiles(_albumDir, "temp.*")
            .Concat(Directory.EnumerateFiles(_albumDir, "out.*"));

        foreach (string file in tempFiles)
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
            }
        }
    }
}