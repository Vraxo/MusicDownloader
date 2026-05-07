using MusicDownloader;

namespace MusicProcessor;

public static class Program
{
    public static void Main()
    {
        // Ensure settings are loaded
        SettingsManager.LoadOrCreate();

        Log.Info("--- Manual FLAC Audio Processor ---");
        Console.WriteLine();

        try
        {
            string? inputFile = UserInput.GetInputFilePath();

            if (inputFile is null)
            {
                return;
            }

            Log.Info("Probing audio file for details...");
            int sampleRate = AudioProber.GetSampleRate(inputFile);

            if (sampleRate <= 0)
            {
                Log.Error("Could not determine audio sample rate. Aborting process.");
                return;
            }

            Log.Info($"Detected sample rate: {sampleRate} Hz");
            Console.WriteLine();

            string tempo = UserInput.GetTempo();
            string range = UserInput.GetTrimRange();

            string outputFile = Path.Combine(
                Path.GetDirectoryName(inputFile)!,
                $"{Path.GetFileNameWithoutExtension(inputFile)}_processed.flac"
            );

            if (File.Exists(outputFile))
            {
                Log.Warning($"Output file '{outputFile}' already exists and will be overwritten.");
            }

            Log.Action("Processing audio...");

            FlacFfmpegCommandBuilder commandBuilder = new(inputFile, outputFile, tempo, range, sampleRate);
            string command = commandBuilder.Build();

            string ffmpegExe = SettingsManager.Current.FfmpegExe;
            int exitCode = ProcessExecutor.Run(ffmpegExe, command);

            Console.WriteLine();
            if (exitCode == 0)
            {
                Log.Success("Successfully processed audio. Output saved to:");
                Log.Success(outputFile);
            }
            else
            {
                Log.Error($"ffmpeg processing failed with exit code {exitCode}.");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"An error occurred: {ex.Message}");
        }
        finally
        {
            Console.WriteLine();
            Log.Info("Press any key to exit...");
            _ = Console.ReadKey();
        }
    }
}