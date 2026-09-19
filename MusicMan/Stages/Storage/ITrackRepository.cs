using MusicMan.Core;

namespace MusicMan.Stages.Storage;

internal interface ITrackRepository
{
    Task<List<Track>> ReadAllTracksAsync();
    Task<Track> UpdateCoverPropertyAsync(Track track, string downloadedCoverPath);
    string GetCanonicalCoverLink(string coverFileName);
}