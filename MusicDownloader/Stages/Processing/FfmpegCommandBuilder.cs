using MusicDownloader.Core;
using MusicDownloader.Infrastructure;
using System.Globalization;

namespace MusicDownloader.Stages.Processing;

internal sealed class FfmpegCommandBuilder(Track track, string inputFile, string outputFile)
{
    public ProcessArguments Build()
    {
        int loopCount = track.Loop;
        string start = track.Range.Count == 2 ? track.Range[0] : string.Empty;
        string end = track.Range.Count == 2 ? track.Range[1] : string.Empty;
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

        args.AddRange(["-i", inputFile]);
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

        args.Add(outputFile);

        return args;
    }

    private string BuildTempoFilterContent()
    {
        if (track.Tempo is null or <= 0)
        {
            return string.Empty;
        }

        double tempoMultiplier = track.Tempo.Value / 100.0;

        if (SettingsManager.Current.PreservePitchWhenChangingTempo)
        {
            return $"atempo={tempoMultiplier.ToString("0.000", CultureInfo.InvariantCulture)}";
        }

        int newSampleRate = (int)double.Round(48000 * tempoMultiplier);
        return $"asetrate={newSampleRate}";
    }
}