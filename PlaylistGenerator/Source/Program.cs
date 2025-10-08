using MusicDownloader;

namespace PlaylistGenerator;

class Program
{
    static void Main()
    {
        try
        {
            Log.Info("Starting playlist generation...");
            PlaylistWriter.GeneratePlaylists();
            Log.Success("All playlists generated successfully.");
        }
        catch (Exception ex)
        {
            Log.Error($"A fatal error occurred: {ex.Message}");
        }

        Console.WriteLine();
        Log.Info("Press any key to exit...");
        Console.ReadKey();
    }
}