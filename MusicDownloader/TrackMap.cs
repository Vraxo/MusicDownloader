using CsvHelper.Configuration;

namespace MusicDownloader;

public class TrackMap : ClassMap<Track>
{
    public TrackMap()
    {
        _ = Map(m => m.Title).Name("title");
        _ = Map(m => m.Artist).Name("artist");
        _ = Map(m => m.Album).Name("album");
        _ = Map(m => m.Url).Name("url");
        _ = Map(m => m.Range).Name("range").Default(string.Empty);
        _ = Map(m => m.Tempo).Name("tempo").Default(string.Empty);
        _ = Map(m => m.Loop).Name("loop").Default("1");
        _ = Map(m => m.TrackNumber).Name("tracknumber").Default(string.Empty);
        _ = Map(m => m.DiscNumber).Name("discnumber").Default(string.Empty);
        _ = Map(m => m.Tags).Name("tags").Convert(args =>
        {
            return !args.Row.TryGetField<string>("tags", out var tagsCsv) || string.IsNullOrWhiteSpace(tagsCsv)
                ? []
                : (IReadOnlyList<string>)tagsCsv.Split(',')
                .Select(tag => tag.Trim())
                .Where(tag => !string.IsNullOrEmpty(tag))
                .ToList();
        });
    }
}