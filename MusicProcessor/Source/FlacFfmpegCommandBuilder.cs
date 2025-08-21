using System.Globalization;

namespace ManualAudioProcessor;

public class FlacFfmpegCommandBuilder
{
    private readonly string _inputFile;
    private readonly string _outputFile;
    private readonly string _tempo;
    private readonly string _range;
    private readonly int _sourceSampleRate;

    public FlacFfmpegCommandBuilder(string inputFile, string outputFile, string tempo, string range, int sourceSampleRate)
    {
        _inputFile = inputFile;
        _outputFile = outputFile;
        _tempo = tempo;
        _range = range;
        _sourceSampleRate = sourceSampleRate;
    }

    public string Build()
    {
        string trimOpts = BuildTrimOptions();
        string filterOpts = BuildFilterOptions();

        // -map_metadata 0 copies all metadata from the first input file.
        // -map 0:a selects the audio stream(s).
        // -map 0:v? selects the video stream (cover art) if it exists, without erroring if it doesn't.
        // -c:a flac ensures lossless audio compression after applying filters.
        // -c:v copy copies the cover art without re-encoding.
        return $"-y {trimOpts} " +
               $"-i \"{_inputFile}\" {filterOpts} " +
               "-map 0:a -map 0:v? " +
               "-map_metadata 0 " +
               "-c:a flac " +
               "-c:v copy " +
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
        if (string.IsNullOrWhiteSpace(_tempo)
            || !double.TryParse(_tempo, NumberStyles.Any, CultureInfo.InvariantCulture, out double tempoPercent)
            || _sourceSampleRate <= 0)
        {
            return "";
        }

        double tempoMultiplier = tempoPercent / 100.0;

        int newSampleRate = (int)double.Round(_sourceSampleRate * tempoMultiplier);
        return $"-filter:a \"asetrate={newSampleRate}\"";
    }
}