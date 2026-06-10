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
    public string Build()
    {
        string trimOpts = BuildTrimOptions();
        string filterOpts = BuildFilterOptions();
        string trimStr = string.IsNullOrEmpty(trimOpts) ? "" : $"{trimOpts} ";

        if (string.IsNullOrEmpty(filterOpts))
        {
            return
                $"-y {trimStr}" +
                $"-i \"{inputFile}\" " +
                $"-map 0 " +
                $"-map_metadata -1 " +
                $"-c copy \"{outputFile}\"";
        }

        return
            $"-y {trimStr}" +
            $"-i \"{inputFile}\" {filterOpts} " +
            $"-map 0 " +
            $"-map_metadata -1 " +
            $"-c:a flac \"{outputFile}\"";
    }

    private string BuildTrimOptions()
    {
        if (range.Count != 2)
        {
            return "";
        }

        string start = range[0];
        string end = range[1];

        List<string> trimArgs = [];
        if (!string.IsNullOrEmpty(start))
        {
            trimArgs.Add($"-ss {start}");
        }

        if (!string.IsNullOrEmpty(end))
        {
            trimArgs.Add($"-to {end}");
        }

        return string.Join(" ", trimArgs);
    }

    private string BuildFilterOptions()
    {
        if (tempo is null or <= 0)
        {
            return "";
        }

        double tempoMultiplier = tempo.Value / 100.0;

        if (SettingsManager.Current.PreservePitchWhenChangingTempo)
        {
            string tempoFormatted = tempoMultiplier.ToString("0.000", CultureInfo.InvariantCulture);
            return $"-filter:a \"atempo={tempoFormatted}\"";
        }

        int newSampleRate = (int)double.Round(sampleRate * tempoMultiplier);
        return $"-filter:a \"asetrate={newSampleRate}\"";
    }
}