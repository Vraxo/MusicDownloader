using MusicDownloader.Common;
using MusicDownloader.Infrastructure;
using MusicDownloader.Workflows;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

try
{
    bool isFirstRun = !Directory.Exists(SettingsManager.Current.DatabaseDir);

    Directory.CreateDirectory(SettingsManager.Current.BaseDataDir);
    Directory.CreateDirectory(SettingsManager.Current.DatabaseDir);

    if (isFirstRun)
    {
        Log.Success($"Created default database directory: '{SettingsManager.Current.DatabaseDir}'");
        Log.Info($"Please place your `.toml` track files in '{SettingsManager.Current.DatabaseDir}' and run the application again.");
    }
    else
    {
        ITrackRepository repository = new MarkdownTrackRepository();

        if (args.Any(a => a.Equals("playlist", StringComparison.OrdinalIgnoreCase)))
        {
            await PlaylistWriter.GeneratePlaylistsAsync(repository);
        }
        else if (args.Any(a => a.Equals("process", StringComparison.OrdinalIgnoreCase)))
        {
            ManualProcessor.Run();
        }
        else
        {
            Log.Info("Starting download processing... (use 'playlist' or 'process' arguments for other tools)");
            await AutomaticProcessor.RunAsync(repository);
        }
    }
}
catch (Exception ex)
{
    Log.Error($"Fatal error: {ex.Message}");
}

Console.WriteLine();
Log.Info("Press any key to exit...");
Console.ReadKey();