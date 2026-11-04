using MusicMan.Core;
using MusicMan.Infrastructure;
using MusicMan.Stages.Storage;
using Spectre.Console;

namespace MusicMan.Orchestration;

internal sealed class CoverArtHandler(Track track, CsvTrackRepository repository)
{
    public Track CurrentTrack { get; private set; } = track;

    public bool CoverExistsLocally()
    {
        if (string.Equals(CurrentTrack.Cover, "none", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

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

        string tempFileBase = Path.Combine(Path.GetDirectoryName(workspace.TempFileBase)!, "temp_thumb");

        try
        {
            AnsiConsole.MarkupLine($"[cyan]Downloading missing cover art from source for: [white]{CurrentTrack.Title.EscapeMarkup()}[/][/]");
            CurrentTrack = await DownloadAndStoreCoverAsync(CurrentTrack, tempFileBase);
        }
        finally
        {
            CleanupTempThumbs(Path.GetDirectoryName(workspace.TempFileBase)!);
        }
    }

    public async Task<Track> ResolveCoverArtAsync(DownloadWorkspace workspace)
    {
        if (string.Equals(CurrentTrack.Cover, "none", StringComparison.OrdinalIgnoreCase))
        {
            return CurrentTrack;
        }

        string coverFileName = PathUtils.GetCoverFileName(CurrentTrack);
        string finalCoverPath = Path.Combine(SettingsManager.Current.CoversDir, coverFileName);

        if (!CoverExistsLocally())
        {
            CurrentTrack = await StoreDownloadedCoverAsync(workspace.TempFileBase);
        }
        else
        {
            Log.Info($"Re-using existing cover art: 'Covers/{coverFileName}'");

            if (!string.IsNullOrWhiteSpace(CurrentTrack.Cover))
            {
                string expectedCoverLink = repository.GetCanonicalCoverLink(coverFileName);
                if (!string.Equals(CurrentTrack.Cover, expectedCoverLink, StringComparison.OrdinalIgnoreCase))
                {
                    CurrentTrack = await repository.UpdateCoverPropertyAsync(CurrentTrack, finalCoverPath);
                }
            }
        }

        return CurrentTrack;
    }

    private async Task<Track> DownloadAndStoreCoverAsync(Track track, string tempFileBase)
    {
        bool downloaded = await DownloadWorkspace.DownloadThumbnailOnlyAsync(track, tempFileBase);
        if (!downloaded)
        {
            Log.Warning($"Failed to download thumbnail for '{track.Title}' from source.");
            return track;
        }

        return await StoreDownloadedCoverAsync(tempFileBase);
    }

    private async Task<Track> StoreDownloadedCoverAsync(string tempFileBase)
    {
        string? downloadedCover = DownloadWorkspace.FindDownloadedCover(tempFileBase);
        if (downloadedCover is null)
        {
            return CurrentTrack;
        }

        // Fitting happens before the file is copied into the covers folder, so the stored cover
        // and the embedded picture stay byte-identical and the up-to-date check keeps working.
        string? fittedCover = null;
        try
        {
            CoverFitter fitter = new(SettingsManager.Current.CoverFit);
            fittedCover = await fitter.TryFitAsync(downloadedCover);

            return await repository.UpdateCoverPropertyAsync(CurrentTrack, fittedCover ?? downloadedCover);
        }
        finally
        {
            CoverFitter.DeleteQuietly(fittedCover);
        }
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
