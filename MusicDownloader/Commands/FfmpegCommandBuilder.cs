using MusicDownloader.Common;
using MusicDownloader.Infrastructure;
using System.Globalization;

namespace MusicDownloader.Commands;

internal sealed class FfmpegCommandBuilder
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
        bool hasTrim = TrackParser.TryParseRange(_track.Range, out string start, out string end);
        string tempoFilter = BuildTempoFilterContent();
        bool hasFilter = !string.IsNullOrEmpty(tempoFilter) || loopCount > 1;

        List<string> args = ["-y"];

        if (hasTrim && loopCount <= 1)
        {
            args.Add($"-ss {start} -to {end}");
        }

        args.Add($"-i \"{_inputFile}\"");

        if (_coverFile is not null)
        {
            args.Add($"-i \"{_coverFile}\"");
        }

        if (loopCount > 1)
        {
            List<string> filterList = [];
            if (hasTrim)
            {
                filterList.Add($"atrim=start={start}:end={end}");
                filterList.Add("asetpts=PTS-STARTPTS");
            }
            filterList.Add($"aloop=loop={loopCount - 1}:size=2147483647");
            if (!string.IsNullOrEmpty(tempoFilter))
            {
                filterList.Add(tempoFilter);
            }

            string filterChain = string.Join(",", filterList);
            args.Add($"-filter_complex \"[0:a]{filterChain}[outa]\"");
            args.Add("-map \"[outa]\"");
        }
        else
        {
            if (!string.IsNullOrEmpty(tempoFilter))
            {
                args.Add($"-filter:a \"{tempoFilter}\"");
            }
            args.Add("-map 0:a");
        }

        args.Add(_coverFile is not null ? "-map 1:0" : "-map 0:v?");

        args.AddRange(BuildMetadataArgs());
        args.Add("-map_metadata -1");

        if (hasFilter)
        {
            args.Add($"-c:a aac -b:a {SettingsManager.Current.AudioBitrateKbps}k");
        }
        else
        {
            args.Add("-c:a copy");
        }

        args.Add(_coverFile is not null ? "-c:v mjpeg -disposition:v:0 attached_pic" : "-c:v copy");
        args.Add($"\"{_outputFile}\"");

        return string.Join(" ", args);
    }

    private int ParseLoopCount()
    {
        return int.TryParse(_track.Loop, out int val) && val > 0 ? val : 1;
    }

    private IEnumerable<string> BuildMetadataArgs()
    {
        List<string> meta = [
            $"-metadata title=\"{_track.Title}\"",
            $"-metadata artist=\"{_track.Artist}\"",
            $"-metadata album=\"{_track.Album}\""
        ];

        if (!string.IsNullOrWhiteSpace(_track.TrackNumber))
        {
            meta.Add($"-metadata track=\"{_track.TrackNumber}\"");
        }

        if (!string.IsNullOrWhiteSpace(_track.DiscNumber))
        {
            meta.Add($"-metadata disc=\"{_track.DiscNumber}\"");
        }

        return meta;
    }

    private string BuildTempoFilterContent()
    {
        if (_track.Tempo is null or <= 0)
        {
            return "";
        }

        double tempoMultiplier = _track.Tempo.Value / 100.0;

        if (SettingsManager.Current.PreservePitchWhenChangingTempo)
        {
            return $"atempo={tempoMultiplier.ToString("0.000", CultureInfo.InvariantCulture)}";
        }

        int newSampleRate = (int)Math.Round(48000 * tempoMultiplier);
        return $"asetrate={newSampleRate}";
    }
}