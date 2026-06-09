using System.ComponentModel;
using System.Diagnostics;

namespace MusicDownloader;

public class ProcessExecutor
{
    // Added optional stdErrHandler to allow callers to override default logging behavior
    public static int Run(string exe, string args, Action<string>? stdErrHandler = null)
    {
        try
        {
            using Process proc = new()
            {
                StartInfo = new()
                {
                    FileName = exe,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            // Capture Standard Output (usually normal logs/data)
            proc.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Log.Info(e.Data);
                }
            };

            // Capture Standard Error
            // If a custom handler is provided (e.g., to suppress red text), use it.
            // Otherwise, use the default heuristic.
            proc.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    if (stdErrHandler is not null)
                    {
                        stdErrHandler(e.Data);
                    }
                    else
                    {
                        HandleStdErr(e.Data);
                    }
                }
            };

            _ = proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();

            return proc.ExitCode;
        }
        catch (Win32Exception)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error($"Unexpected error running '{exe}': {ex.Message}");
            throw;
        }
    }

    private static void HandleStdErr(string data)
    {
        // Heuristic: Check for keywords to decide log color.
        // Casing is ignored for robustness.
        if (data.Contains("error", StringComparison.OrdinalIgnoreCase) ||
            data.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
            data.Contains("fatal", StringComparison.OrdinalIgnoreCase))
        {
            // Real errors stay Red.
            if (data.Contains("WARNING", StringComparison.OrdinalIgnoreCase))
            {
                Log.Warning(data);
            }
            else
            {
                Log.Error(data);
            }
        }
        else if (data.Contains("warning", StringComparison.OrdinalIgnoreCase))
        {
            // Warnings become Yellow.
            Log.Warning(data);
        }
        else
        {
            // Everything else (progress bars, stats, ffmpeg info) stays Gray/Info.
            Log.Info(data);
        }
    }
}