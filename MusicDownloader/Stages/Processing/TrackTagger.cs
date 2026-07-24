using MusicDownloader.Core;
using MusicDownloader.Infrastructure;

namespace MusicDownloader.Stages.Processing;

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
                string coverFileName = PathUtils.GetCoverFileName(track);
                string coverPath = Path.Combine(SettingsManager.Current.CoversDir, coverFileName);

                if (File.Exists(coverPath))
                {
                    byte[] bytes = File.ReadAllBytes(coverPath);

                    TagLib.Picture basePicture = new()
                    {
                        Type = TagLib.PictureType.FrontCover,
                        MimeType = coverPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg",
                        Description = coverFileName,
                        Filename = coverFileName,
                        Data = [.. bytes]
                    };

                    bool isOggOrFlac = filePath.EndsWith(".opus", StringComparison.OrdinalIgnoreCase)
                                    || filePath.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase)
                                    || filePath.EndsWith(".flac", StringComparison.OrdinalIgnoreCase);

                    TagLib.IPicture finalPicture = isOggOrFlac
                        ? new TagLib.Flac.Picture(basePicture)
                        : basePicture;

                    file.Tag.Pictures = [finalPicture];
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
            Log.Error($"Failed to apply metadata tags to '{Path.GetFileName(filePath)}':\n{ex}");
            return false;
        }
    }
}