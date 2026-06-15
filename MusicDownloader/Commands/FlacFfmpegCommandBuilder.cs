using MusicDownloader.Infrastructure;
using System.Globalization;

namespace MusicDownloader.Commands;

internal sealed class FlacFfmpegCommandBuilder(
    string inputFile,
    string outputFile,
    double? tempo,
    IReadOnlyList<string> range,
    int sampleRate)
{
    public ProcessArguments Build()
    {
        List<string> args = ["-y", "-v", "error"];

        args.AddRange(BuildTrimOptions());

        args.AddRange(["-i", inputFile]);

        args.AddRange(BuildFilterOptions());

        args.AddRange(["-map", "0", "-map_metadata", "-1"]);

        if (HasFilter())
        {
            args.AddRange(["-c:a", "flac"]);
        }
        else
        {
            args.AddRange(["-c", "copy"]);
        }

        args.Add(outputFile);

        return args;
    }

    private bool HasFilter()
    {
        return tempo is > 0;
    }

    private List<string> BuildTrimOptions()
    {
        if (range.Count != 2)
        {
            return [];
        }

        string start = range[0];
        string end = range[1];

        List<string> trimArgs = [];
        if (!string.IsNullOrEmpty(start))
        {
            trimArgs.AddRange(["-ss", start]);
        }

        if (!string.IsNullOrEmpty(end))
        {
            trimArgs.AddRange(["-to", end]);
        }

        return trimArgs;
    }

    private List<string> BuildFilterOptions()
    {
        if (tempo is null or <= 0)
        {
            return [];
        }

        double tempoMultiplier = tempo.Value / 100.0;

        if (SettingsManager.Current.PreservePitchWhenChangingTempo)
        {
            string tempoFormatted = tempoMultiplier.ToString("0.000", CultureInfo.InvariantCulture);
            return ["-filter:a", $"atempo={tempoFormatted}"];
        }

        int newSampleRate = (int)double.Round(sampleRate * tempoMultiplier);
        return ["-filter:a", $"asetrate={newSampleRate}"];
    }
}