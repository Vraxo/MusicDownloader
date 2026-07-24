using MusicDownloader.Core;
using MusicDownloader.Infrastructure;
using MusicDownloader.Stages.Storage;

namespace MusicDownloader.Orchestration;

internal sealed class QueueProcessor(ITrackRepository repository)
{
    public async Task<(int Downloaded, int MetadataUpdated, int Failed, int UpToDate)> ProcessAsync(List<Track> queue, int alreadyDownloadedCount)
    {
        int downloaded = 0;
        int metadataUpdated = 0;
        int failed = 0;
        int upToDate = alreadyDownloadedCount;
        int total = queue.Count;

        for (int i = 0; i < total; i++)
        {
            Track track = queue[i];
            TrackProcessStatus status = TrackProcessStatus.Failed;

            try
            {
                TrackProcessor trackProcessor = new(track, i + 1, total, repository);
                status = await trackProcessor.ProcessAsync();
            }
            catch (Exception ex)
            {
                Log.Error($"Processing failed for track '{track.Title}': {ex.Message}");
            }

            switch (status)
            {
                case TrackProcessStatus.Success:
                    downloaded++;
                    break;
                case TrackProcessStatus.Failed:
                    failed++;
                    break;
                case TrackProcessStatus.Skipped:
                    upToDate++;
                    break;
                case TrackProcessStatus.MetadataUpdated:
                    metadataUpdated++;
                    break;
            }

            if (status != TrackProcessStatus.Skipped)
            {
                Console.WriteLine();
            }

            bool downloadAttempted = status == TrackProcessStatus.Success;
            if (downloadAttempted && SettingsManager.Current.DelayBetweenDownloadsMs > 0)
            {
                await Task.Delay(SettingsManager.Current.DelayBetweenDownloadsMs);
            }
        }

        return (downloaded, metadataUpdated, failed, upToDate);
    }
}