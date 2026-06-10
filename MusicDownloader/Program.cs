using MusicDownloader.Common;
using MusicDownloader.Infrastructure;
using MusicDownloader.Workflows;

try
{
    Directory.CreateDirectory(SettingsManager.Current.BaseDataDir);

    if (args.Any(a => a.Equals("playlist", StringComparison.OrdinalIgnoreCase)))
    {
        PlaylistWriter.GeneratePlaylists();
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