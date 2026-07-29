using MusicDownloader.Core;
using MusicDownloader.Infrastructure;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MusicDownloader.Stages.Storage;

internal sealed class MarkdownTrackRepository : ITrackRepository
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitDefaults | DefaultValuesHandling.OmitEmptyCollections)
        .Build();

    public string GetCanonicalCoverLink(string coverFileName)
    {
        return $"[[Covers/{coverFileName}]]";
    }

    public async Task<List<Track>> ReadAllTracksAsync()
    {
        string dbDir = SettingsManager.Current.DatabaseDir;

        if (!Directory.Exists(dbDir))
        {
            Log.Error($"Database directory '{dbDir}' not found.");
            return [];
        }

        List<string> mdFiles = [.. Directory.EnumerateFiles(dbDir, "*.md", SearchOption.AllDirectories)];

        if (mdFiles.Count == 0)
        {
            Log.Warning($"No .md files found in '{dbDir}'.");
            return [];
        }

        Log.Info("Verifying database file formatting...");

        List<Track> tracks = [];
        int successfullyLoadedFiles = 0;
        int reformattedCount = 0;

        foreach (string mdFile in mdFiles)
        {
            (List<Track> fileTracks, bool wasReformatted) = await GetTracksFromSingleMarkdownAsync(mdFile);
            if (fileTracks.Count == 0)
            {
                continue;
            }

            if (wasReformatted)
            {
                reformattedCount++;
            }

            tracks.AddRange(fileTracks);
            successfullyLoadedFiles++;
        }

        if (reformattedCount > 0)
        {
            Log.Success($"Reformatted {reformattedCount} database collections.");
        }
        else
        {
            Log.Info("All database collections are already perfectly formatted.");
        }

        if (tracks.Count > 0)
        {
            Log.Success($"Successfully loaded {tracks.Count} tracks from {successfullyLoadedFiles} collections.");
        }

        return tracks;
    }

    public async Task<Track> UpdateCoverPropertyAsync(Track track, string downloadedCoverPath)
    {
        if (string.IsNullOrEmpty(track.DatabaseFilePath) || !File.Exists(track.DatabaseFilePath))
        {
            return track;
        }

        string ext = Path.GetExtension(downloadedCoverPath).ToLowerInvariant();
        string coversDir = SettingsManager.Current.CoversDir;
        string destinationFileName;

        if (!string.IsNullOrWhiteSpace(track.Cover))
        {
            destinationFileName = PathUtils.GetCoverFileName(track);
        }
        else
        {
            string safeTitle = PathUtils.SafeFileName(track.Title);
            destinationFileName = $"{safeTitle}{ext}";
        }

        try
        {
            Directory.CreateDirectory(coversDir);
            string destinationPath = Path.Combine(coversDir, destinationFileName);

            if (Path.GetFullPath(downloadedCoverPath) != Path.GetFullPath(destinationPath))
            {
                File.Copy(downloadedCoverPath, destinationPath, overwrite: true);
            }

            string content = await File.ReadAllTextAsync(track.DatabaseFilePath);
            (Track parsedTrack, string body) = Parse(content);

            string expectedCoverLink = GetCanonicalCoverLink(destinationFileName);
            Track updatedTrack = parsedTrack with { Cover = expectedCoverLink };
            string formatted = Format(updatedTrack, body);

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

    private async Task<(List<Track> Tracks, bool WasReformatted)> GetTracksFromSingleMarkdownAsync(string filePath)
    {
        try
        {
            string content = await File.ReadAllTextAsync(filePath);
            (Track track, string body) = Parse(content, filePath);

            if (string.IsNullOrWhiteSpace(track.Source))
            {
                return ([], false);
            }

            Track trackWithFile = track with { DatabaseFilePath = filePath };

            string formatted = Format(trackWithFile, body);
            string contentNormalized = content.Replace("\r\n", "\n").Trim();
            string formattedNormalized = formatted.Replace("\r\n", "\n").Trim();
            bool wasReformatted = false;

            if (!string.Equals(contentNormalized, formattedNormalized, StringComparison.Ordinal))
            {
                await File.WriteAllTextAsync(filePath, formatted);
                Log.Info($"Updated formatting for '{Path.GetFileName(filePath)}'");
                wasReformatted = true;
            }

            return ([trackWithFile], wasReformatted);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to read or parse Markdown file '{filePath}': {ex.Message}");
            return ([], false);
        }
    }

    private (Track Track, string Body) Parse(string fileContent, string? filePath = null)
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

    private string Format(Track track, string body)
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