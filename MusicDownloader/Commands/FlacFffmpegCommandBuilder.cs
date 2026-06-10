using MusicDownloader.Common;
using MusicDownloader.Infrastructure;
using System.Globalization;

namespace MusicDownloader.Commands;

public class FlacFfmpegCommandBuilder
{
    private readonly string _inputFile;
    private readonly string _outputFile;
    private readonly string _tempo;
    private readonly string _range;
    private readonly int _sampleRate;

    public FlacFfmpegCommandBuilder(string inputFile, string outputFile, string tempo, string range, int sampleRate)
    {
        _inputFile = inputFile;
        _outputFile = outputFile;
        _tempo = tempo;
        _range = range;
        _sampleRate = sampleRate;
    }

    public string Build()
    {
        string trimOpts = BuildTrimOptions();
        string filterOpts = BuildFilterOptions();

        return !string.IsNullOrEmpty(filterOpts)
            ? $"-y {trimOpts} -i \"{_inputFile}\" {filterOpts} -map 0 -map_metadata -1 -c:a flac \"{_outputFile}\""
            : $"-y {trimOpts} -i \"{_inputFile}\" -map 0 -map_metadata -1 -c copy \"{_outputFile}\"";
    }

    private string BuildTrimOptions()
    {
        return TrackParser.TryParseRange(_range, out string start, out string end)
            ? $"-ss {start} -to {end}"
            : "";
    }

    private string BuildFilterOptions()
    {
        if (!TrackParser.TryParseTempo(_tempo, out double tempoMultiplier))
        {
            return "";
        }

        if (SettingsManager.Current.PreservePitchWhenChangingTempo)
        {
            string tempoFormatted = tempoMultiplier.ToString("0.000", CultureInfo.InvariantCulture);
            return $"-filter:a \"atempo={tempoFormatted}\"";
        }

        int newSampleRate = (int)double.Round(_sampleRate * tempoMultiplier);
        return $"-filter:a \"asetrate={newSampleRate}\"";
    }
}