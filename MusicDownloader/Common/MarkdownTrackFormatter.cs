using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MusicDownloader.Common;

internal static class MarkdownTrackFormatter
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitDefaults | DefaultValuesHandling.OmitEmptyCollections)
        .Build();

    public static (Track Track, string Body) Parse(string fileContent, string? filePath = null)
    {
        string normalized = fileContent.Replace("\r\n", "\n");
        if (!normalized.StartsWith("---"))
        {
            return (new Track(), normalized);
        }

        int secondDelimiter = normalized.IndexOf("\n---", 3);
        if (secondDelimiter == -1)
        {
            return (new Track(), normalized);
        }

        string frontmatterBlock = normalized[3..secondDelimiter].Trim('\n', '\r');
        string body = normalized[(secondDelimiter + 4)..].TrimStart('\n', '\r');

        try
        {
            Track track = Deserializer.Deserialize<Track>(frontmatterBlock);
            return (track ?? new Track(), body);
        }
        catch (Exception ex)
        {
            if (filePath is not null)
            {
                Log.Warning($"Failed to parse YAML frontmatter in '{Path.GetFileName(filePath)}': {ex.Message}");
            }
            return (new Track(), body);
        }
    }

    public static string Format(Track track, string body)
    {
        Track trackToSerialize = track with
        {
            Tags = [.. track.Tags.OrderBy(t => t, StringComparer.OrdinalIgnoreCase)]
        };

        string yaml = Serializer.Serialize(trackToSerialize).TrimEnd('\n', '\r');

        StringBuilder sb = new();
        sb.AppendLine("---");
        sb.AppendLine(yaml);
        sb.AppendLine("---");

        string trimmedBody = body.TrimStart('\r', '\n');

        if (trimmedBody.StartsWith("![[Covers/", StringComparison.OrdinalIgnoreCase))
        {
            int closingBrackets = trimmedBody.IndexOf("]]");
            if (closingBrackets != -1)
            {
                trimmedBody = trimmedBody[(closingBrackets + 2)..].TrimStart('\r', '\n');
            }
        }

        if (!string.IsNullOrWhiteSpace(track.Cover))
        {
            string coverEmbedLink = $"![[{track.Cover.Replace("[[", "").Replace("]]", "")}]]";
            sb.AppendLine();
            sb.AppendLine(coverEmbedLink);
        }

        if (!string.IsNullOrWhiteSpace(trimmedBody))
        {
            if (string.IsNullOrWhiteSpace(track.Cover))
            {
                sb.AppendLine();
            }
            sb.Append(trimmedBody);
        }

        return sb.ToString();
    }
}