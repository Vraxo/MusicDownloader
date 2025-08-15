namespace MusicDownloader;

public static class Program
{
    public static void Main()
    {
        Log.Info("Starting playlist check...");

        try
        {
            Dictionary<string, List<string>> songToPlaylistsMap = PlaylistService.BuildSongPlaylistMap();

            if (songToPlaylistsMap.Count > 0)
            {
                ReportGenerator reportGenerator = new();
                (string reportContent, ReportSummary summary) = reportGenerator.Generate(songToPlaylistsMap);

                ReportWriter reportWriter = new();
                ReportWriter.Write(reportContent, summary);
            }
        }
        catch (DirectoryNotFoundException)
        {
            Log.Warning($"Base data directory '{AppSettings.BaseDataDir}' not found. Skipping playlist check.");
        }
        catch (Exception ex)
        {
            Log.Error($"An error occurred during playlist check: {ex.Message}");
        }
    }
}