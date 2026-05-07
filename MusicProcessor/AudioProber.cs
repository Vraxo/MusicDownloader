using MusicDownloader;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace MusicProcessor;

public static class AudioProber
{
    public static int GetSampleRate(string inputFile)
    {
        return RunProbe(inputFile, "stream=sample_rate", (output) =>
        {
            return int.TryParse(output, out int val)
                ? val
                : -1;
        });
    }

    public static double GetDuration(string inputFile)
    {
        return RunProbe(inputFile, "format=duration", (output) =>
        {
            return double.TryParse(output, NumberStyles.Any, CultureInfo.InvariantCulture, out double val)
            ? val
            : -1.0;
        });
    }

    private static T RunProbe<T>(string inputFile, string entries, Func<string, T> parser)
    {
        string ffprobeExe = SettingsManager.Current.FfmpegExe.Replace("ffmpeg", "ffprobe");
        string ffprobePath = ExecutableFinder.GetFullPath(ffprobeExe, SettingsManager.Current.FfmpegDir);

        string args = $"-v error -select_streams a:0 -show_entries {entries} -of default=noprint_wrappers=1:nokey=1 \"{inputFile}\"";

        StringBuilder outputBuilder = new();

        try
        {
            using Process proc = new()
            {
                StartInfo = new()
                {
                    FileName = ffprobePath,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            proc.OutputDataReceived += (_, e) =>
            {
                if (e.Data is null)
                {
                    return;
                }

                _ = outputBuilder.Append(e.Data);
            };

            _ = proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine(); // Consume stderr to prevent blocking
            proc.WaitForExit();

            return proc.ExitCode != 0
                ? parser("")
                : parser(outputBuilder.ToString().Trim());
        }
        catch
        {
            return parser("");
        }
    }
}