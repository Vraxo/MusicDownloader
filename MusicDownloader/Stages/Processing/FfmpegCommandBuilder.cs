using MusicDownloader.Common;
using MusicDownloader.Infrastructure;
using System.Globalization;

namespace MusicDownloader.Commands;

internal sealed class FfmpegCommandBuilder(Track track, string inputFile, string outputFile)
{
    private readonly Track _track = track;
    private readonly string _inputFile = inputFile;
    private readonly string _outputFile = outputFile;

    public ProcessArguments Build()
    {
        int loopCount = _track.Loop;
        string start = _track.Range.Count == 2 ? _track.Range[0] : string.Empty;
        string end = _track.Range.Count == 2 ? _track.Range[1] : string.Empty;
        bool hasTrim = !string.IsNullOrEmpty(start) || !string.IsNullOrEmpty(end);
        string tempoFilter = BuildTempoFilterContent();
        bool hasFilter = !string.IsNullOrEmpty(tempoFilter) || loopCount > 1;

        List<string> args = ["-y", "-v", "error"];

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
        args.Add("-vn");

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

            args.AddRange(["-filter_complex", $"[0:a]{string.Join(",", filterList)}[outa]", "-map", "[outa]"]);
        }
        else
        {
            if (!string.IsNullOrEmpty(tempoFilter))
            {
                args.AddRange(["-filter:a", tempoFilter]);
            }
            args.AddRange(["-map", "0:a"]);
        }

        if (hasFilter)
        {
            args.AddRange(["-c:a", "libopus", "-b:a", "160k"]);
        }
        else
        {
            args.AddRange(["-c:a", "copy"]);
        }

        args.Add(_outputFile);

        return args;
    }

    private string BuildTempoFilterContent()
    {
        if (_track.Tempo is null or <= 0)
        {
            return string.Empty;
        }

        double tempoMultiplier = _track.Tempo.Value / 100.0;

        if (SettingsManager.Current.PreservePitchWhenChangingTempo)
        {
            return $"atempo={tempoMultiplier.ToString("0.000", CultureInfo.InvariantCulture)}";
        }

        int newSampleRate = (int)double.Round(48000 * tempoMultiplier);
        return $"asetrate={newSampleRate}";
    }
}