using MusicDownloader.Commands;
using MusicDownloader.Common;
using MusicDownloader.Infrastructure;
using System.Globalization;

namespace MusicDownloader.Workflows;

internal static class ManualProcessor
{
    public static void Run()
    {
        Log.Info("--- Manual FLAC Audio Processor ---");
        Console.WriteLine();

        string? inputFile = UserInput.GetInputFilePath();
        if (inputFile is null)
        {
            return;
        }

        int sampleRate = ProbeSampleRate(inputFile);
        if (sampleRate <= 0)
        {
            return;
        }

        string tempoInput = UserInput.GetTempo();
        double? tempo = null;
        if (!string.IsNullOrWhiteSpace(tempoInput) && double.TryParse(tempoInput, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedTempo))
        {
            tempo = parsedTempo;
        }

        string rangeInput = UserInput.GetTrimRange();
        IReadOnlyList<string> range = [];

        if (!string.IsNullOrWhiteSpace(rangeInput))
        {
            string[] parts = rangeInput.Split('-');
            if (parts.Length == 2)
            {
                range = [parts[0].Trim(), parts[1].Trim()];
            }
        }

        string outputFile = ResolveOutputFile(inputFile);

        Log.Action("Processing audio...");
        ExecuteProcessing(inputFile, outputFile, tempo, range, sampleRate);
    }

    private static int ProbeSampleRate(string inputFile)
    {
        Log.Info("Probing audio file for details...");
        int sampleRate = AudioProber.GetSampleRate(inputFile);

        if (sampleRate <= 0)
        {
            Log.Error("Could not determine audio sample rate. Aborting process.");
            return -1;
        }

        Log.Info($"Detected sample rate: {sampleRate} Hz");
        Console.WriteLine();
        return sampleRate;
    }

    private static string ResolveOutputFile(string inputFile)
    {
        string directory = Path.GetDirectoryName(inputFile) ?? string.Empty;
        string fileName = Path.GetFileNameWithoutExtension(inputFile);
        string outputFile = Path.Combine(directory, $"{fileName}_processed.flac");

        if (File.Exists(outputFile))
        {
            Log.Warning($"Output file '{outputFile}' already exists and will be overwritten.");
        }

        return outputFile;
    }

    private static void ExecuteProcessing(string inputFile, string outputFile, double? tempo, IReadOnlyList<string> range, int sampleRate)
    {
        FlacFfmpegCommandBuilder commandBuilder = new(inputFile, outputFile, tempo, range, sampleRate);
        ProcessArguments command = commandBuilder.Build();

        string ffmpegPath = ExecutableFinder.GetFullPath(SettingsManager.Current.FfmpegExe, SettingsManager.Current.FfmpegDir);
        int exitCode = ProcessExecutor.Run(ffmpegPath, command);

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
}