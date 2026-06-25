using System.Globalization;
using System.Text;

namespace MusicDownloader.Common;

internal static class TomlTrackFormatter
{
    public static string Format(SongCollection collection)
    {
        if (collection.Song is null || collection.Song.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new();

        for (int i = 0; i < collection.Song.Count; i++)
        {
            Track track = collection.Song[i];

            builder.AppendLine("[[Song]]");
            AppendString(builder, "Source", track.Source);
            AppendString(builder, "Artist", track.Artist);
            AppendString(builder, "Album", track.Album);
            AppendString(builder, "Title", track.Title);
            AppendNumber(builder, "TrackNumber", track.TrackNumber);
            AppendNumber(builder, "DiscNumber", track.DiscNumber);
            AppendString(builder, "Date", track.Date);
            AppendArray(builder, "Tags", track.Tags);
            AppendArray(builder, "Range", track.Range);
            AppendNumber(builder, "Tempo", track.Tempo);

            if (track.Loop != 1)
            {
                builder.AppendLine($"Loop = {track.Loop}");
            }

            if (i < collection.Song.Count - 1)
            {
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    private static void AppendString(StringBuilder builder, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            builder.AppendLine($"{key} = \"{value.Replace("\"", "\\\"")}\"");
        }
    }

    private static void AppendNumber(StringBuilder builder, string key, double? value)
    {
        if (value.HasValue)
        {
            builder.AppendLine($"{key} = {value.Value.ToString(CultureInfo.InvariantCulture)}");
        }
    }

    private static void AppendArray(StringBuilder builder, string key, IReadOnlyList<string>? values)
    {
        if (values is not null && values.Count > 0)
        {
            string formattedValues = string.Join(", ", values.Select(v => $"\"{v.Replace("\"", "\\\"")}\""));
            builder.AppendLine($"{key} = [{formattedValues}]");
        }
    }
}