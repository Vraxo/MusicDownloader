using CsvHelper.Configuration;

namespace MusicDownloader;

public class TrackMap : ClassMap<Track>
{
    public TrackMap()
    {
        Map(m => m.Title).Name("title");
        Map(m => m.Artist).Name("artist");
        Map(m => m.Album).Name("album");
        Map(m => m.Url).Name("url");
        Map(m => m.Range).Name("range").Default(string.Empty);
        Map(m => m.Tempo).Name("tempo").Default(string.Empty);
        Map(m => m.TrackNumber).Name("tracknumber").Default(string.Empty);
        Map(m => m.DiscNumber).Name("discnumber").Default(string.Empty);
        Map(m => m.Tags).Name("tags").Convert(args =>
        {
            if (!args.Row.TryGetField<string>("tags", out var tagsCsv) || string.IsNullOrWhiteSpace(tagsCsv))
            {
                return [];
            }

            return tagsCsv.Split(',')
                .Select(tag => tag.Trim())
                .Where(tag => !string.IsNullOrEmpty(tag))
                .ToList();
        });
    }
}