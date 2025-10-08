using System.Globalization;
using System.Text;

namespace MusicDownloader;

public class FfmpegCommandBuilder
{
    private readonly Track _track;
    private readonly string _inputFile;
    private readonly string _outputFile;
    private readonly string _filterOpts;

    public FfmpegCommandBuilder(Track track, string inputFile, string outputFile)
    {
        _track = track;
        _inputFile = inputFile;
        _outputFile = outputFile;
        _filterOpts = BuildFilterOptions();
    }

    public string Build()
    {
        string trimOpts = BuildTrimOptions();

        if (!string.IsNullOrEmpty(_filterOpts))
        {
            return BuildFilterCommand(trimOpts);
        }

        return BuildSimpleCopyCommand(trimOpts);
    }

    private string BuildFilterCommand(string trimOpts)
    {
        return $"-y {trimOpts} " +
               $"-i \"{_inputFile}\" {_filterOpts} " +
               "-map 0:a -map 0:v " +
               BuildMetadataArgs() +
               "-map_metadata -1 " +
               $"-c:a aac -b:a {AppSettings.AudioBitrateKbps}k " +
               "-c:v copy " +
               $"\"{_outputFile}\"";
    }

    private string BuildSimpleCopyCommand(string trimOpts)
    {
        return $"-y {trimOpts} " +
               $"-i \"{_inputFile}\" " +
               "-map 0 " +
               BuildMetadataArgs() +
               "-map_metadata -1 " +
               "-c copy " +
               $"\"{_outputFile}\"";
    }

    private string BuildMetadataArgs()
    {
        var builder = new StringBuilder();
        builder.Append($"-metadata title=\"{_track.Title}\" ");
        builder.Append($"-metadata artist=\"{_track.Artist}\" ");
        builder.Append($"-metadata album=\"{_track.Album}\" ");

        if (!string.IsNullOrWhiteSpace(_track.TrackNumber))
        {
            builder.Append($"-metadata track=\"{_track.TrackNumber}\" ");
        }

        if (!string.IsNullOrWhiteSpace(_track.DiscNumber))
        {
            builder.Append($"-metadata disc=\"{_track.DiscNumber}\" ");
        }

        return builder.ToString();
    }

    private string BuildTrimOptions()
    {
        if (string.IsNullOrWhiteSpace(_track.Range))
        {
            return "";
        }

        string[] parts = _track.Range.Split('-');
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
        if (string.IsNullOrWhiteSpace(_track.Tempo) || !double.TryParse(_track.Tempo, NumberStyles.Any, CultureInfo.InvariantCulture, out double tempoPercent))
        {
            return "";
        }

        double tempoMultiplier = tempoPercent / 100.0;

        if (AppSettings.PreservePitchWhenChangingTempo)
        {
            string tempoFormatted = tempoMultiplier.ToString("0.000", CultureInfo.InvariantCulture);
            return $"-filter:a \"atempo={tempoFormatted}\"";
        }
        else
        {
            // Pre-calculate the new sample rate to avoid issues with ffmpeg's expression parser.
            // We assume a 48000Hz source rate, which is standard for web audio and confirmed in logs.
            int newSampleRate = (int)Math.Round(48000 * tempoMultiplier);
            return $"-filter:a \"asetrate={newSampleRate}\"";
        }
    }
}