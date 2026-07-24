namespace MusicDownloader.Common;

internal interface ITrackRepository
{
    Task<List<Track>> ReadAllTracksAsync();
    Task<Track> UpdateCoverPropertyAsync(Track track, string downloadedCoverPath);
}