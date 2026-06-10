using MusicDownloader.Common;
using MusicDownloader.Infrastructure;
using System.Globalization;

namespace MusicDownloader.Commands;

internal sealed class FlacFfmpegCommandBuilder(
    string inputFile,
    string outputFile,
    double? tempo,
    string range,
    int sampleRate)
{
    public string Build()
    {
        string trimOpts = BuildTrimOptions();
        string filterOpts = BuildFilterOptions();

        if (string.IsNullOrEmpty(filterOpts))
        {
            return
                $"-y {trimOpts} " +
                $"-i \"{inputFile}\" " +
                $"-map 0 " +
                $"-map_metadata -1 " +
                $"-c copy \"{outputFile}\"";
        }

        return
            $"-y {trimOpts} " +
            $"-i \"{inputFile}\" {filterOpts} " +
            $"-map 0 " +
            $"-map_metadata -1 " +
            $"-c:a flac \"{outputFile}\"";
    }

    private string BuildTrimOptions()
    {
        return TrackParser.TryParseRange(range, out string start, out string end)
            ? $"-ss {start} -to {end}"
            : "";
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