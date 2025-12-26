using System.Globalization;
using System.Text;

namespace MusicDownloader;

public class FfmpegCommandBuilder
{
    private readonly Track _track;
    private readonly string _inputFile;
    private readonly string _outputFile;

    public FfmpegCommandBuilder(Track track, string inputFile, string outputFile)
    {
        _track = track;
        _inputFile = inputFile;
        _outputFile = outputFile;
    }

    public string Build()
    {
        int loopCount = ParseLoopCount();

        // If Loop is > 1, we MUST use a complex filter chain to handle Trim -> Loop -> Tempo correctly.
        if (loopCount > 1)
        {
            return BuildComplexLoopCommand(loopCount);
        }

        // Otherwise, use the standard simple build logic.
        string filterOpts = BuildTempoFilter();
        string trimOpts = BuildTrimArgs();

        return !string.IsNullOrEmpty(filterOpts)
            ? BuildSimpleFilterCommand(trimOpts, filterOpts)
            : BuildSimpleCopyCommand(trimOpts);
    }

    private int ParseLoopCount()
    {
        return string.IsNullOrWhiteSpace(_track.Loop) ? 1 : int.TryParse(_track.Loop, out int val) && val > 0 ? val : 1;
    }

    // --- LOOP MODE (Complex Filter) ---

    private string BuildComplexLoopCommand(int loopCount)
    {
        // Chain: [0:a] -> atrim (Optional) -> asetpts -> aloop -> atempo (Optional) -> [outa]
        List<string> filters = [];

        // 1. Trim
        if (TryGetTrimTimes(out string start, out string end))
        {
            filters.Add($"atrim=start={start}:end={end}");
            filters.Add("asetpts=PTS-STARTPTS");
        }

        // 2. Loop
        // 'loop' filter takes the number of REPEATS. So playing 2 times means loop=1.
        // size=2e9 allows looping up to ~12 hours of audio buffer, which is safe for music.
        filters.Add($"aloop=loop={loopCount - 1}:size=2147483647");

        // 3. Tempo
        string tempoFilter = BuildTempoFilterContent();
        if (!string.IsNullOrEmpty(tempoFilter))
        {
            filters.Add(tempoFilter);
        }

        string filterChain = string.Join(",", filters);

        // We map [outa] to output. We also copy video (cover art) if possible, or ignore it if not mapped.
        // Note: We use -map 0:v? to copy cover art if present, but ignore failures.
        return $"-y -i \"{_inputFile}\" " +
               $"-filter_complex \"[0:a]{filterChain}[outa]\" " +
               "-map \"[outa]\" -map 0:v? " +
               BuildMetadataArgs() +
               "-map_metadata -1 " +
               $"-c:a aac -b:a {SettingsManager.Current.AudioBitrateKbps}k " +
               "-c:v copy " +
               $"\"{_outputFile}\"";
    }

    // --- STANDARD MODE (Simple Args) ---

    private string BuildSimpleFilterCommand(string trimOpts, string filterOpts)
    {
        return $"-y {trimOpts} " +
               $"-i \"{_inputFile}\" {filterOpts} " +
               "-map 0:a -map 0:v " +
               BuildMetadataArgs() +
               "-map_metadata -1 " +
               $"-c:a aac -b:a {SettingsManager.Current.AudioBitrateKbps}k " +
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

    // --- HELPERS ---

    private string BuildMetadataArgs()
    {
        StringBuilder builder = new();
        _ = builder.Append($"-metadata title=\"{_track.Title}\" ");
        _ = builder.Append($"-metadata artist=\"{_track.Artist}\" ");
        _ = builder.Append($"-metadata album=\"{_track.Album}\" ");

        if (!string.IsNullOrWhiteSpace(_track.TrackNumber))
        {
            _ = builder.Append($"-metadata track=\"{_track.TrackNumber}\" ");
        }

        if (!string.IsNullOrWhiteSpace(_track.DiscNumber))
        {
            _ = builder.Append($"-metadata disc=\"{_track.DiscNumber}\" ");
        }

        return builder.ToString();
    }

    private string BuildTrimArgs()
    {
        return !TryGetTrimTimes(out string start, out string end) ? "" : $"-ss {start} -to {end}";
    }

    private bool TryGetTrimTimes(out string start, out string end)
    {
        start = "";
        end = "";

        if (string.IsNullOrWhiteSpace(_track.Range))
        {
            return false;
        }

        string[] parts = _track.Range.Split('-');
        if (parts.Length != 2)
        {
            return false;
        }

        start = parts[0].Trim();
        end = parts[1].Trim();

        return !string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end);
    }

    private string BuildTempoFilter()
    {
        string content = BuildTempoFilterContent();
        return !string.IsNullOrEmpty(content) ? $"-filter:a \"{content}\"" : "";
    }

    private string BuildTempoFilterContent()
    {
        if (string.IsNullOrWhiteSpace(_track.Tempo) || !double.TryParse(_track.Tempo, NumberStyles.Any, CultureInfo.InvariantCulture, out double tempoPercent))
        {
            return "";
        }

        double tempoMultiplier = tempoPercent / 100.0;

        if (SettingsManager.Current.PreservePitchWhenChangingTempo)
        {
            string tempoFormatted = tempoMultiplier.ToString("0.000", CultureInfo.InvariantCulture);
            return $"atempo={tempoFormatted}";
        }
        else
        {
            int newSampleRate = (int)Math.Round(48000 * tempoMultiplier);
            return $"asetrate={newSampleRate}";
        }
    }
}