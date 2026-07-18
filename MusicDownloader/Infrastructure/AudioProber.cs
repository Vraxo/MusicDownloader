using MusicDownloader.Common;
using Spectre.Console;

namespace MusicDownloader.Infrastructure;

internal static class AudioProber
{
    public static int GetSampleRate(string inputFile)
    {
        try
        {
            using TagLib.File file = TagLib.File.Create(inputFile);
            return file.Properties.AudioSampleRate;
        }
        catch
        {
            return -1;
        }
    }

    public static bool IsMetadataUpToDate(string filePath, Track track, out string? mismatchReason)
    {
        try
        {
            using TagLib.File file = TagLib.File.Create(filePath);
            TagLib.Tag tag = file.Tag;
            List<string> mismatches = [];

            CheckField(tag.Title, track.Title, "Title", mismatches);
            CheckField(tag.FirstPerformer, track.Artist, "Artist", mismatches);
            CheckField(tag.FirstAlbumArtist, track.AlbumArtist ?? string.Empty, "AlbumArtist", mismatches);
            CheckField(tag.FirstComposer, track.Composer ?? string.Empty, "Composer", mismatches);
            CheckField(tag.Album, track.Album, "Album", mismatches);

            uint trackNum = (uint)(track.TrackNumber ?? 0);
            if (tag.Track != trackNum)
            {
                CheckField(tag.Track.ToString(), trackNum.ToString(), "Track", mismatches);
            }

            uint discNum = (uint)(track.DiscNumber ?? 0);
            if (tag.Disc != discNum)
            {
                CheckField(tag.Disc.ToString(), discNum.ToString(), "Disc", mismatches);
            }

            uint expectedYear = 0;
            if (!string.IsNullOrWhiteSpace(track.Date) && track.Date.Length >= 4 && uint.TryParse(track.Date[..4], out uint parsedYear))
            {
                expectedYear = parsedYear;
            }
            if (tag.Year != expectedYear)
            {
                CheckField(tag.Year.ToString(), expectedYear.ToString(), "Date", mismatches);
            }

            string expectedGenre = track.Tags.Count > 0 ? string.Join(", ", track.Tags) : string.Empty;
            string actualGenre = tag.Genres.Length > 0 ? string.Join(", ", tag.Genres) : string.Empty;
            CheckField(actualGenre, expectedGenre, "Genre", mismatches);

            CheckField(tag.Comment, track.Source, "Comment/Source", mismatches);

            bool hasCover = tag.Pictures.Length > 0;
            bool expectsCover = !string.IsNullOrWhiteSpace(track.Cover);

            if (expectsCover)
            {
                string coverFileName = PathUtils.GetCoverFileName(track);
                string coverPath = Path.Combine(SettingsManager.Current.CoversDir, coverFileName);

                if (File.Exists(coverPath))
                {
                    if (!hasCover)
                    {
                        mismatches.Add("[gray]    - Cover Art: '[/][red]Missing[/][gray]' -> '[/][green]Expected[/][gray]'[/]");
                    }
                    else
                    {
                        long diskFileSize = new FileInfo(coverPath).Length;
                        long embeddedSize = tag.Pictures[0].Data.Count;

                        if (diskFileSize != embeddedSize)
                        {
                            mismatches.Add("[gray]    - Cover Art: '[/][red]Outdated Image[/][gray]' -> '[/][green]New Image Detected[/][gray]'[/]");
                        }
                    }
                }
            }
            else if (hasCover)
            {
                mismatches.Add("[gray]    - Cover Art: '[/][red]Present[/][gray]' -> '[/][green]None Expected[/][gray]'[/]");
            }

            if (mismatches.Count > 0)
            {
                mismatchReason = string.Join(Environment.NewLine, mismatches);
                return false;
            }

            mismatchReason = null;
            return true;
        }
        catch
        {
            mismatchReason = "Failed to read existing audio metadata.";
            return false;
        }
    }

    private static void CheckField(string? actualValue, string expectedValue, string displayName, List<string> mismatches)
    {
        string cleanActual = (actualValue ?? string.Empty).Trim().Replace("\r", "").Replace("\n", "");
        string cleanExpected = expectedValue.Trim().Replace("\r", "").Replace("\n", "");

        if (string.Equals(cleanActual, cleanExpected, StringComparison.Ordinal))
        {
            return;
        }

        mismatches.Add($"[gray]    - {displayName}: '[/][red]{cleanActual.EscapeMarkup()}[/][gray]' -> '[/][green]{cleanExpected.EscapeMarkup()}[/][gray]'[/]");
    }
}