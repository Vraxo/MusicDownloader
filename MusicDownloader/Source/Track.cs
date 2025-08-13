namespace MusicDownloader;

public class Track
{
    public string Title { get; }
    public string Artist { get; }
    public string Album { get; }
    public string Url { get; }
    public string Range { get; }
    public string Tempo { get; }

    public Track(IReadOnlyList<string> fields)
    {
        Title = Clean(fields[0]);
        Artist = Clean(fields[1]);
        Album = Clean(fields[2]);
        Url = Clean(fields[3]);
        Range = Clean(fields[4]);
        Tempo = Clean(fields[5]);
    }

    public static string SafeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return name;
    }

    private static string Clean(string s)
    {
        return s.Trim();
    }
}