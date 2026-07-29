using MusicDownloader.Core;
using MusicDownloader.Infrastructure;
using MusicDownloader.Stages.Storage;
using Spectre.Console;

namespace MusicDownloader.Orchestration;

internal sealed class CoverArtHandler(Track track, ITrackRepository repository)
{
    public Track CurrentTrack { get; private set; } = track;

    public bool CoverExistsLocally()
    {
        string coverFileName = PathUtils.GetCoverFileName(CurrentTrack);
        string coverPath = Path.Combine(SettingsManager.Current.CoversDir, coverFileName);
        return File.Exists(coverPath);
    }

    public async Task EnsureCoverArtExistsAsync(DownloadWorkspace workspace)
    {
        if (CoverExistsLocally() || string.IsNullOrWhiteSpace(CurrentTrack.Source))
        {
            return;
        }

        string coverFileName = PathUtils.GetCoverFileName(CurrentTrack);
        string tempFileBase = Path.Combine(Path.GetDirectoryName(workspace.TempFileBase)!, "temp_thumb");

        try
        {
            AnsiConsole.MarkupLine($"[cyan]Downloading missing cover art from source for: [white]{CurrentTrack.Title.EscapeMarkup()}[/][/]");
            bool downloaded = await workspace.DownloadThumbnailOnlyAsync(CurrentTrack, tempFileBase);
            if (downloaded)
            {
                string? downloadedCover = workspace.FindDownloadedCover(tempFileBase);
                if (downloadedCover is not null)
                {
                    CurrentTrack = await repository.UpdateCoverPropertyAsync(CurrentTrack, downloadedCover);
                    Log.Success($"Successfully downloaded and placed cover art: '{coverFileName}'");
                }
            }
            else
            {
                Log.Warning($"Failed to download thumbnail for '{CurrentTrack.Title}' from source.");
            }
        }
        finally
        {
            CleanupTempThumbs(Path.GetDirectoryName(workspace.TempFileBase)!);
        }
    }

    public async Task<Track> ResolveCoverArtAsync(DownloadWorkspace workspace)
    {
        string coverFileName = PathUtils.GetCoverFileName(CurrentTrack);
        string finalCoverPath = Path.Combine(SettingsManager.Current.CoversDir, coverFileName);

        if (!CoverExistsLocally())
        {
            string? downloadedCover = workspace.FindDownloadedCover(workspace.TempFileBase);
            if (downloadedCover is not null)
            {
                CurrentTrack = await repository.UpdateCoverPropertyAsync(CurrentTrack, downloadedCover);
            }
        }
        else
        {
            Log.Info($"Re-using existing cover art: 'Covers/{coverFileName}'");

            string expectedCoverLink = repository.GetCanonicalCoverLink(coverFileName);
            if (!string.Equals(CurrentTrack.Cover, expectedCoverLink, StringComparison.OrdinalIgnoreCase))
            {
                CurrentTrack = await repository.UpdateCoverPropertyAsync(CurrentTrack, finalCoverPath);
            }
        }

        return CurrentTrack;
    }

    private static void CleanupTempThumbs(string dir)
    {
        if (!Directory.Exists(dir))
        {
            return;
        }

        IEnumerable<string> tempFiles = Directory.EnumerateFiles(dir, "temp_thumb.*");
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