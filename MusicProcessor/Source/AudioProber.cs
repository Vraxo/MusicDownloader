using MusicDownloader;
using System.Diagnostics;
using System.Text;

namespace ManualAudioProcessor;

public static class AudioProber
{
    public static int GetSampleRate(string inputFile)
    {
        string ffprobeExe = AppSettings.FfmpegExe.Replace("ffmpeg", "ffprobe");

        string args = $"-v error -select_streams" +
            $"a:0 " +
            $"-show_entries stream=sample_rate " +
            $"-of default=noprint_wrappers=1:nokey=1 \"{inputFile}\"";

        StringBuilder outputBuilder = new();
        int exitCode = -1;

        try
        {
            using Process proc = new()
            {
                StartInfo = new()
                {
                    FileName = ffprobeExe,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            object lockObj = new();

            proc.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                {
                    lock (lockObj)
                    {
                        outputBuilder.Append(e.Data);
                    }
                }
            };

            proc.ErrorDataReceived += (_, e) => 
            { 
                if (!string.IsNullOrEmpty(e.Data)) 
                {
                    Log.Error($"ffprobe error: {e.Data}");
                }
            };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();

            exitCode = proc.ExitCode;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            Log.Error($"'{ffprobeExe}' not found." +
                $"Ensure it is in the same directory as '{AppSettings.FfmpegExe}' or in your system's PATH.");
            return -1;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to run ffprobe: {ex.Message}");
            return -1;
        }

        if (exitCode != 0)
        {
            Log.Error("ffprobe failed to get sample rate.");
            return -1;
        }

        if (int.TryParse(outputBuilder.ToString().Trim(), out int sampleRate))
        {
            return sampleRate;
        }

        Log.Error("Could not parse sample rate from ffprobe output.");
        return -1;
    }
}