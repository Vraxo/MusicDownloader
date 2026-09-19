using MusicMan.Infrastructure;
using MusicMan.Orchestration;
using MusicMan.Stages.Storage;
using System.Text;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        try
        {
            string csvPath = Path.Combine(SettingsManager.Current.DatabaseDir, "tracks.csv");
            bool isFirstRun = !File.Exists(csvPath);

            Directory.CreateDirectory(SettingsManager.Current.BaseDataDir);
            Directory.CreateDirectory(SettingsManager.Current.DatabaseDir);

            CsvTrackRepository repository = new();

            if (isFirstRun)
            {
                await repository.ReadAllTracksAsync();

                Log.Success($"Created default database directory: '{SettingsManager.Current.DatabaseDir}'");
                Log.Info($"Please populate your tracks in CSV format at '{csvPath}' and run the application again.");
            }
            else
            {
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
    }
}