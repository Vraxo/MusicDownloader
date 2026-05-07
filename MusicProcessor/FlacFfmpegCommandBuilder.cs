using MusicDownloader;
using System.Globalization;

namespace MusicProcessor;

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
            ? $"-y {trimOpts} " +
                   $"-i \"{_inputFile}\" {filterOpts} " +
                   "-map 0 " +
                   "-map_metadata -1 " +
                   "-c:a flac " +
                   $"\"{_outputFile}\""
            : $"-y {trimOpts} " +
               $"-i \"{_inputFile}\" " +
               "-map 0 " +
               "-map_metadata -1 " +
               "-c copy " +
               $"\"{_outputFile}\"";
    }

    private string BuildTrimOptions()
    {
        if (string.IsNullOrWhiteSpace(_range))
        {
            return "";
        }

        string[] parts = _range.Split('-');
        if (parts.Length != 2)
        {
            return "";
        }

        string start = parts[0].Trim();
        string end = parts[1].Trim();

        return !string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end)
            ? $"-ss {start} -to {end}"
            : "";
    }

    private string BuildFilterOptions()
    {
        if (string.IsNullOrWhiteSpace(_tempo) || !double.TryParse(_tempo, NumberStyles.Any, CultureInfo.InvariantCulture, out double tempoPercent))
        {
            return "";
        }

        double tempoMultiplier = tempoPercent / 100.0;

        if (SettingsManager.Current.PreservePitchWhenChangingTempo)
        {
            string tempoFormatted = tempoMultiplier.ToString("0.000", CultureInfo.InvariantCulture);
            return $"-filter:a \"atempo={tempoFormatted}\"";
        }
        else
        {
            int newSampleRate = (int)Math.Round(_sampleRate * tempoMultiplier);
            return $"-filter:a \"asetrate={newSampleRate}\"";
        }
    }
}