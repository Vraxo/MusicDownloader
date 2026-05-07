using System.Globalization;
using System.Text;

namespace MusicDownloader;

public class FfmpegCommandBuilder
{
    private readonly Track _track;
    private readonly string _inputFile;
    private readonly string _outputFile;
    private readonly string? _coverFile;

    public FfmpegCommandBuilder(Track track, string inputFile, string outputFile, string? coverFile = null)
    {
        _track = track;
        _inputFile = inputFile;
        _outputFile = outputFile;
        _coverFile = coverFile;
    }

    public string Build()
    {
        int loopCount = ParseLoopCount();

        // If Loop is > 1, we MUST use a complex filter chain.
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
        filters.Add($"aloop=loop={loopCount - 1}:size=2147483647");

        // 3. Tempo
        string tempoFilter = BuildTempoFilterContent();
        if (!string.IsNullOrEmpty(tempoFilter))
        {
            filters.Add(tempoFilter);
        }

        string filterChain = string.Join(",", filters);

        // Inputs
        StringBuilder cmd = new();
        _ = cmd.Append($"-y -i \"{_inputFile}\" ");

        if (_coverFile is not null)
        {
            _ = cmd.Append($"-i \"{_coverFile}\" ");
        }

        // Filter Complex
        _ = cmd.Append($"-filter_complex \"[0:a]{filterChain}[outa]\" ");

        // Maps
        _ = cmd.Append("-map \"[outa]\" ");

        if (_coverFile is not null)
        {
            _ = cmd.Append("-map 1:0 "); // Map the cover art from second input
        }
        else
        {
            _ = cmd.Append("-map 0:v? "); // Try to map internal cover if exists
        }

        // Metadata & Codecs
        _ = cmd.Append(BuildMetadataArgs());
        _ = cmd.Append("-map_metadata -1 ");
        _ = cmd.Append($"-c:a aac -b:a {SettingsManager.Current.AudioBitrateKbps}k ");

        if (_coverFile is not null)
        {
            // Force MJPEG for compatibility (WebP covers break many players)
            _ = cmd.Append("-c:v mjpeg -disposition:v:0 attached_pic ");
        }
        else
        {
            _ = cmd.Append("-c:v copy ");
        }

        _ = cmd.Append($"\"{_outputFile}\"");

        return cmd.ToString();
    }

    // --- STANDARD MODE (Simple Args) ---

    private string BuildSimpleFilterCommand(string trimOpts, string filterOpts)
    {
        StringBuilder cmd = new();
        _ = cmd.Append($"-y {trimOpts} -i \"{_inputFile}\" ");

        if (_coverFile is not null)
        {
            _ = cmd.Append($"-i \"{_coverFile}\" ");
        }

        _ = cmd.Append($"{filterOpts} ");

        // Maps
        _ = cmd.Append("-map 0:a "); // Map audio from first input

        if (_coverFile is not null)
        {
            _ = cmd.Append("-map 1:0 "); // Map cover from second input
        }
        else
        {
            _ = cmd.Append("-map 0:v? "); // Fallback
        }

        // Metadata & Codecs
        _ = cmd.Append(BuildMetadataArgs());
        _ = cmd.Append("-map_metadata -1 ");
        _ = cmd.Append($"-c:a aac -b:a {SettingsManager.Current.AudioBitrateKbps}k ");

        _ = _coverFile is not null ? cmd.Append("-c:v mjpeg -disposition:v:0 attached_pic ") : cmd.Append("-c:v copy ");

        _ = cmd.Append($"\"{_outputFile}\"");

        return cmd.ToString();
    }

    private string BuildSimpleCopyCommand(string trimOpts)
    {
        StringBuilder cmd = new();
        _ = cmd.Append($"-y {trimOpts} -i \"{_inputFile}\" ");

        if (_coverFile is not null)
        {
            _ = cmd.Append($"-i \"{_coverFile}\" ");
        }

        // Maps
        _ = cmd.Append("-map 0:a ");

        _ = _coverFile is not null ? cmd.Append("-map 1:0 ") : cmd.Append("-map 0:v? ");

        // Metadata & Codecs
        _ = cmd.Append(BuildMetadataArgs());
        _ = cmd.Append("-map_metadata -1 ");

        // Even in "copy" mode, we might need to re-encode audio if we are mixing inputs,
        // but here we asked for simple copy. However, if we are adding a cover, we can't just "-c copy" globally.
        // We will copy audio and MJPEG the video.
        _ = cmd.Append("-c:a copy ");

        _ = _coverFile is not null ? cmd.Append("-c:v mjpeg -disposition:v:0 attached_pic ") : cmd.Append("-c:v copy ");

        _ = cmd.Append($"\"{_outputFile}\"");

        return cmd.ToString();
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