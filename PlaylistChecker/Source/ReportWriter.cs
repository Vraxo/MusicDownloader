namespace MusicDownloader;

public class ReportWriter
{
    public static void Write(string reportContent, ReportSummary summary)
    {
        try
        {
            File.WriteAllText(AppSettings.PlaylistCheckReportFile, reportContent);
            Log.Success($"Report successfully saved to '{AppSettings.PlaylistCheckReportFile}'");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to write report file: {ex.Message}");
        }

        Log.Action("\n--- Report Summary (also saved to file) ---");
        Log.Info($"Total Songs: {summary.TotalSongs}");
        Log.Success($"Songs in at least one playlist: {summary.SongsInPlaylists}");

        if (summary.SongsNotInPlaylists > 0)
        {
            Log.Warning($"Songs not in any playlist: {summary.SongsNotInPlaylists}");
        }
        else
        {
            Log.Success("All songs are in at least one playlist!");
        }
    }
}