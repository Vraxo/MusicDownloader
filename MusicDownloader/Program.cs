using MusicDownloader.Common;
using MusicDownloader.Infrastructure;
using MusicDownloader.Workflows;

try
{
    bool isFirstRun = !Directory.Exists(SettingsManager.Current.DatabaseDir);

    _ = Directory.CreateDirectory(SettingsManager.Current.BaseDataDir);
    _ = Directory.CreateDirectory(SettingsManager.Current.DatabaseDir);

    if (isFirstRun)
    {
        Log.Success($"Created default database directory: '{SettingsManager.Current.DatabaseDir}'");
        Log.Info($"Please place your `.toml` track files in '{SettingsManager.Current.DatabaseDir}' and run the application again.");
    }
    else if (args.Any(a => a.Equals("playlist", StringComparison.OrdinalIgnoreCase)))
    {
        await PlaylistWriter.GeneratePlaylistsAsync();
    }
    else if (args.Any(a => a.Equals("process", StringComparison.OrdinalIgnoreCase)))
    {
        ManualProcessor.Run();
    }
    else
    {
        Log.Info("Starting download processing... (use 'playlist' or 'process' arguments for other tools)");
        await AutomaticProcessor.RunAsync();
        Log.Success("All downloads and processing finished.");
    }
}
catch (Exception ex)
{
    Log.Error($"Fatal error: {ex.Message}");
}

Console.WriteLine();
Log.Info("Press any key to exit...");
Console.ReadKey();