namespace MusicDownloader;

public class TrackProcessor
{
    private readonly Track _track;
    private readonly string _albumDir;

    public TrackProcessor(Track track)
    {
        _track = track;
        _albumDir = Path.Combine(AppSettings.BaseDataDir, Track.SafeFileName(_track.Album));
    }

    public async Task ProcessAsync()
    {
        Directory.CreateDirectory(_albumDir);

        string outputFile = Path.Combine(_albumDir, Track.SafeFileName(_track.Title) + $".{AppSettings.AudioFormat}");
        
        if (File.Exists(outputFile))
        {
            Log.Info($"Skipping \"{_track.Title}\" — already exists.");
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

        int exitCode = await Task.Run(() => ProcessExecutor.RunWithFallback(
            AppSettings.YtDlpExe,
            AppSettings.FallbackYtDlpExe,
            command
        ));

        if (exitCode != 0)
        {
            Log.Error($"yt-dlp failed for {_track.Title}, skipping...");
            return false;
        }

        return true;
    }

    private async Task<bool> ProcessAudioAsync(string inputFile, string outputFile)
    {
        Log.Action($"Processing: {_track.Title}");
        var command = new FfmpegCommandBuilder(_track, inputFile, outputFile).Build();

        int exitCode = await Task.Run(() => ProcessExecutor.Run(AppSettings.FfmpegExe, command));

        if (exitCode != 0)
        {
            Log.Error($"ffmpeg processing failed for {_track.Title}, skipping...");
            return false;
        }

        return true;
    }

    private void CleanupTempFiles()
    {
        // Delete any files starting with "temp." or "out.", regardless of extension.
        // This is more robust and cleans up intermediate files (e.g., temp.webm)
        // that yt-dlp might create before conversion.
        IEnumerable<string> tempFiles = Directory.EnumerateFiles(_albumDir, "temp.*")
            .Concat(Directory.EnumerateFiles(_albumDir, "out.*"));

        foreach (var file in tempFiles)
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
                // Ignore errors if the file is locked or doesn't exist.
            }
        }
    }
}