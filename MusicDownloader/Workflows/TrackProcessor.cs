using MusicDownloader.Commands;
using MusicDownloader.Common;
using MusicDownloader.Infrastructure;
using Spectre.Console;
using System.ComponentModel;

namespace MusicDownloader.Workflows;

internal class TrackProcessor
{
    private Track _track;
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
        return Path.Combine(albumDir, PathUtils.SafeFileName(track.Title) + ".opus");
    }

    public async Task<TrackProcessStatus> ProcessAsync()
    {
        Directory.CreateDirectory(_albumDir);
        string outputFile = GetOutputFile(_track);

        if (File.Exists(outputFile))
        {
            return await HandleExistingFileAsync(outputFile);
        }

        return await DownloadAndProcessNewTrackAsync(outputFile);
    }

    private async Task<TrackProcessStatus> HandleExistingFileAsync(string outputFile)
    {
        if (AudioProber.IsMetadataUpToDate(outputFile, _track, out string? mismatch))
        {
            return TrackProcessStatus.Skipped;
        }

        bool updated = await UpdateMetadataInPlaceAsync(outputFile);
        if (updated)
        {
            AnsiConsole.MarkupLine($"{GetLogPrefix().EscapeMarkup()}[green]Updated metadata: [white]{_track.Title.EscapeMarkup()}[/][/]");
            if (!string.IsNullOrEmpty(mismatch))
            {
                AnsiConsole.MarkupLine(mismatch);
            }
        }

        return updated
            ? TrackProcessStatus.MetadataUpdated
            : TrackProcessStatus.Failed;
    }

    private async Task<TrackProcessStatus> DownloadAndProcessNewTrackAsync(string outputFile)
    {
        string tempFileBase = Path.Combine(_albumDir, "temp");
        string finalTempOut = Path.Combine(_albumDir, "out.opus");

        string coverFileName = string.IsNullOrWhiteSpace(_track.Cover)
            ? $"{PathUtils.SafeFileName(_track.Title)}.png"
            : Path.GetFileName(_track.Cover.Replace("[[", "").Replace("]]", "").Replace("/", "\\"));

        Directory.CreateDirectory(SettingsManager.Current.CoversDir);
        string finalCoverPath = Path.Combine(SettingsManager.Current.CoversDir, coverFileName);
        bool coverExistsLocally = File.Exists(finalCoverPath);

        try
        {
            AnsiConsole.MarkupLine($"{GetLogPrefix().EscapeMarkup()}[cyan]Downloading & processing: [white]{_track.Title.EscapeMarkup()}[/][/]");

            if (!await RunFullDownloadAsync(tempFileBase, downloadThumbnail: !coverExistsLocally))
            {
                return TrackProcessStatus.Failed;
            }

            string? downloadedAudio = FindDownloadedFile(tempFileBase);
            if (downloadedAudio is null)
            {
                AnsiConsole.MarkupLine("[red]Download reported success, but no audio file was found.[/]");
                return TrackProcessStatus.Failed;
            }

            if (!coverExistsLocally)
            {
                string? downloadedCover = FindCoverFile(tempFileBase);
                if (downloadedCover is not null)
                {
                    string actualExt = Path.GetExtension(downloadedCover).ToLowerInvariant();
                    coverFileName = Path.GetFileNameWithoutExtension(coverFileName) + actualExt;
                    finalCoverPath = Path.Combine(SettingsManager.Current.CoversDir, coverFileName);

                    File.Copy(downloadedCover, finalCoverPath, overwrite: true);
                    Log.Info($"Saved stand-alone cover art to: 'Covers/{coverFileName}'");
                }
            }
            else
            {
                Log.Info($"Re-using existing cover art: 'Covers/{coverFileName}'");
            }

            string expectedCoverLink = $"[[Covers/{coverFileName}]]";
            if (!string.Equals(_track.Cover, expectedCoverLink, StringComparison.OrdinalIgnoreCase))
            {
                await UpdateMarkdownCoverPropertyAsync(expectedCoverLink);
            }

            if (!await ProcessAudioAsync(_track, downloadedAudio, finalTempOut))
            {
                return TrackProcessStatus.Failed;
            }

            if (!ApplyMetadata(finalTempOut))
            {
                return TrackProcessStatus.Failed;
            }

            File.Move(finalTempOut, outputFile, true);
            AnsiConsole.MarkupLine($"[green]Done[/] -> {outputFile.EscapeMarkup()}");
            return TrackProcessStatus.Success;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed processing: {ex.Message.EscapeMarkup()}[/]");
            return TrackProcessStatus.Failed;
        }
        finally
        {
            CleanupTempFiles();
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
            return Path.GetExtension(f).ToLowerInvariant()
                is not ".webp"
                and not ".jpg"
                and not ".png"
                and not ".json"
                and not ".part"
                and not ".ytdl";
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

    private async Task<bool> RunFullDownloadAsync(string tempFileBase, bool downloadThumbnail)
    {
        ProcessArguments command = new YtDlpCommandBuilder(_track, tempFileBase, downloadThumbnail).Build();
        string ytDlpPath = ExecutableFinder.GetFullPath(SettingsManager.Current.YtDlpExe, SettingsManager.Current.YtDlpDir);

        try
        {
            int exitCode = await Task.Run(() => ProcessExecutor.Run(ytDlpPath, command));

            if (exitCode != 0)
            {
                AnsiConsole.MarkupLine("[red]Download failed.[/]");
                return false;
            }

            return true;
        }
        catch (Win32Exception)
        {
            AnsiConsole.MarkupLine($"[red]Could not find '{SettingsManager.Current.YtDlpExe}'.[/]");
            return false;
        }
    }

    private static async Task<bool> ProcessAudioAsync(Track track, string inputFile, string outputFile)
    {
        ProcessArguments command = new FfmpegCommandBuilder(track, inputFile, outputFile).Build();
        string ffmpegPath = ExecutableFinder.GetFullPath(SettingsManager.Current.FfmpegExe, SettingsManager.Current.FfmpegDir);

        try
        {
            int exitCode = await Task.Run(() => ProcessExecutor.Run(ffmpegPath, command));

            if (exitCode != 0)
            {
                AnsiConsole.MarkupLine("[red]ffmpeg processing failed.[/]");
                return false;
            }
        }
        catch (Win32Exception)
        {
            AnsiConsole.MarkupLine($"[red]Could not find '{SettingsManager.Current.FfmpegExe}'.[/]");
            return false;
        }

        return true;
    }

    private bool ApplyMetadata(string filePath)
    {
        try
        {
            using TagLib.File file = TagLib.File.Create(filePath);

            file.Tag.Title = _track.Title ?? string.Empty;
            file.Tag.Performers = string.IsNullOrWhiteSpace(_track.Artist) ? [] : [_track.Artist];
            file.Tag.AlbumArtists = string.IsNullOrWhiteSpace(_track.AlbumArtist) ? [] : [_track.AlbumArtist];
            file.Tag.Composers = string.IsNullOrWhiteSpace(_track.Composer) ? [] : [_track.Composer];
            file.Tag.Album = _track.Album ?? string.Empty;
            file.Tag.Track = (uint)(_track.TrackNumber ?? 0);
            file.Tag.Disc = (uint)(_track.DiscNumber ?? 0);

            if (!string.IsNullOrWhiteSpace(_track.Date) && _track.Date.Length >= 4 && uint.TryParse(_track.Date[..4], out uint year))
            {
                file.Tag.Year = year;
            }

            file.Tag.Genres = _track.Tags?.ToArray() ?? [];
            file.Tag.Comment = _track.Source ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(_track.Cover))
            {
                string cleanLink = _track.Cover.Replace("[[", "").Replace("]]", "").Replace("/", "\\");
                string coverFileName = Path.GetFileName(cleanLink);
                string coverPath = Path.Combine(SettingsManager.Current.CoversDir, coverFileName);

                if (File.Exists(coverPath))
                {
                    file.Tag.Pictures = [new TagLib.Picture(coverPath)];
                }
            }

            file.Save();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to apply metadata tags: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> UpdateMetadataInPlaceAsync(string outputFile)
    {
        return await Task.Run(() => ApplyMetadata(outputFile));
    }

    private async Task UpdateMarkdownCoverPropertyAsync(string expectedCoverLink)
    {
        if (string.IsNullOrEmpty(_track.DatabaseFilePath) || !File.Exists(_track.DatabaseFilePath))
        {
            return;
        }

        try
        {
            string content = await File.ReadAllTextAsync(_track.DatabaseFilePath);
            (Track parsedTrack, string body) = MarkdownTrackFormatter.Parse(content);

            Track updatedTrack = parsedTrack with { Cover = expectedCoverLink };
            string formatted = MarkdownTrackFormatter.Format(updatedTrack, body);

            await File.WriteAllTextAsync(_track.DatabaseFilePath, formatted);

            _track = updatedTrack with { DatabaseFilePath = _track.DatabaseFilePath };
            Log.Success($"Linked database to: '{expectedCoverLink}'");
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to update markdown cover property: {ex.Message}");
        }
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