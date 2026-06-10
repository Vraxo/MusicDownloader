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

    public ProcessArguments Build()
    {
        int loopCount = _track.Loop;
        string start = _track.Range.Count == 2 ? _track.Range[0] : "";
        string end = _track.Range.Count == 2 ? _track.Range[1] : "";
        bool hasTrim = !string.IsNullOrEmpty(start) || !string.IsNullOrEmpty(end);
        string tempoFilter = BuildTempoFilterContent();
        bool hasFilter = !string.IsNullOrEmpty(tempoFilter) || loopCount > 1;

        List<string> args = ["-y"];

        if (hasTrim && loopCount <= 1)
        {
            if (!string.IsNullOrEmpty(start))
            {
                args.AddRange(["-ss", start]);
            }

            if (!string.IsNullOrEmpty(end))
            {
                args.AddRange(["-to", end]);
            }
        }

        args.AddRange(["-i", _inputFile]);

        if (_coverFile is not null)
        {
            args.AddRange(["-i", _coverFile]);
        }

        if (loopCount > 1)
        {
            List<string> filterList = [];
            if (hasTrim)
            {
                List<string> atrimList = [];
                if (!string.IsNullOrEmpty(start))
                {
                    atrimList.Add($"start={start}");
                }

                if (!string.IsNullOrEmpty(end))
                {
                    atrimList.Add($"end={end}");
                }

                filterList.Add($"atrim={string.Join(":", atrimList)}");
                filterList.Add("asetpts=PTS-STARTPTS");
            }
            filterList.Add($"aloop=loop={loopCount - 1}:size=2147483647");
            if (!string.IsNullOrEmpty(tempoFilter))
            {
                filterList.Add(tempoFilter);
            }

            string filterChain = string.Join(",", filterList);
            args.AddRange(["-filter_complex", $"[0:a]{filterChain}[outa]", "-map", "[outa]"]);
        }
        else
        {
            if (!string.IsNullOrEmpty(tempoFilter))
            {
                args.AddRange(["-filter:a", tempoFilter]);
            }
            args.AddRange(["-map", "0:a"]);
        }

        if (_coverFile is not null)
        {
            args.AddRange(["-map", "1:0"]);
        }
        else
        {
            args.AddRange(["-map", "0:v?"]);
        }

        args.AddRange(BuildMetadataArgs());
        args.AddRange(["-map_metadata", "-1"]);

        if (hasFilter)
        {
            args.AddRange(["-c:a", "aac", "-b:a", $"{SettingsManager.Current.AudioBitrateKbps}k"]);
        }
        else
        {
            args.AddRange(["-c:a", "copy"]);
        }

        if (_coverFile is not null)
        {
            args.AddRange(["-c:v", "mjpeg", "-disposition:v:0", "attached_pic"]);
        }
        else
        {
            args.AddRange(["-c:v", "copy"]);
        }

        args.Add(_outputFile);

        return args;
    }

    private List<string> BuildMetadataArgs()
    {
        List<string> meta = [];

        meta.AddRange(["-metadata", $"title={_track.Title}"]);
        meta.AddRange(["-metadata", $"artist={_track.Artist}"]);
        meta.AddRange(["-metadata", $"album={_track.Album}"]);

        if (_track.TrackNumber.HasValue)
        {
            meta.AddRange(["-metadata", $"track={_track.TrackNumber.Value}"]);
        }

        if (_track.DiscNumber.HasValue)
        {
            meta.AddRange(["-metadata", $"disc={_track.DiscNumber.Value}"]);
        }

        if (_track.Date.HasValue)
        {
            meta.AddRange(["-metadata", $"date={_track.Date.Value:yyyy-MM-dd}"]);
        }

        if (_track.Tags.Count > 0)
        {
            string tagsJoined = string.Join(", ", _track.Tags);
            meta.AddRange(["-metadata", $"genre={tagsJoined}"]);
        }

        if (!string.IsNullOrWhiteSpace(_track.Source))
        {
            meta.AddRange(["-metadata", $"comment={_track.Source}"]);
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