using MusicDownloader.Common;
using MusicDownloader.Infrastructure;

namespace MusicDownloader.Workflows;

internal static class TrackTagger
{
    public static bool Apply(Track track, string filePath)
    {
        try
        {
            using TagLib.File file = TagLib.File.Create(filePath);

            file.Tag.Title = track.Title ?? string.Empty;
            file.Tag.Performers = string.IsNullOrWhiteSpace(track.Artist) ? [] : [track.Artist];
            file.Tag.AlbumArtists = string.IsNullOrWhiteSpace(track.AlbumArtist) ? [] : [track.AlbumArtist];
            file.Tag.Composers = string.IsNullOrWhiteSpace(track.Composer) ? [] : [track.Composer];
            file.Tag.Album = track.Album ?? string.Empty;
            file.Tag.Track = (uint)(track.TrackNumber ?? 0);
            file.Tag.Disc = (uint)(track.DiscNumber ?? 0);

            if (!string.IsNullOrWhiteSpace(track.Date) && track.Date.Length >= 4 && uint.TryParse(track.Date[..4], out uint year))
            {
                file.Tag.Year = year;
            }

            file.Tag.Genres = track.Tags?.ToArray() ?? [];
            file.Tag.Comment = track.Source ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(track.Cover))
            {
                string cleanLink = track.Cover.Replace("[[", "").Replace("]]", "").Replace("/", "\\");
                string coverFileName = Path.GetFileName(cleanLink);
                string coverPath = Path.Combine(SettingsManager.Current.CoversDir, coverFileName);

                if (File.Exists(coverPath))
                {
                    file.Tag.Pictures = [new TagLib.Picture(coverPath)];
                }
                else
                {
                    file.Tag.Pictures = [];
                }
            }
            else
            {
                file.Tag.Pictures = [];
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

    public static async Task<Track> UpdateMarkdownCoverPropertyAsync(Track track, string downloadedCoverPath)
    {
        if (string.IsNullOrEmpty(track.DatabaseFilePath) || !File.Exists(track.DatabaseFilePath))
        {
            return track;
        }

        string ext = Path.GetExtension(downloadedCoverPath).ToLowerInvariant();
        string safeTitle = PathUtils.SafeFileName(track.Title);
        string coversDir = SettingsManager.Current.CoversDir;

        try
        {
            Directory.CreateDirectory(coversDir);
            string destinationFileName = $"{safeTitle}{ext}";
            string destinationPath = Path.Combine(coversDir, destinationFileName);

            if (Path.GetFullPath(downloadedCoverPath) != Path.GetFullPath(destinationPath))
            {
                File.Copy(downloadedCoverPath, destinationPath, overwrite: true);
            }

            string content = await File.ReadAllTextAsync(track.DatabaseFilePath);
            (Track parsedTrack, string body) = MarkdownTrackFormatter.Parse(content);

            string expectedCoverLink = $"[[Covers/{destinationFileName}]]";
            Track updatedTrack = parsedTrack with { Cover = expectedCoverLink };
            string formatted = MarkdownTrackFormatter.Format(updatedTrack, body);

            await File.WriteAllTextAsync(track.DatabaseFilePath, formatted);
            Log.Success($"Linked database to: '{expectedCoverLink}'");

            return updatedTrack with { DatabaseFilePath = track.DatabaseFilePath };
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to update markdown cover property: {ex.Message}");
            return track;
        }
    }
}