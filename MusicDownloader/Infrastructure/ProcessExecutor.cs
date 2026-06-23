using MusicDownloader.Common;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace MusicDownloader.Infrastructure;

internal class ProcessExecutor
{
    public static int Run(string exe, ProcessArguments args, Action<string>? stdErrHandler = null)
    {
        try
        {
            using Process proc = CreateProcess(exe, args);

            proc.OutputDataReceived += (_, e) =>
            {
                if (string.IsNullOrEmpty(e.Data) || IsProgressLine(e.Data) || IsNoiseLine(e.Data))
                {
                    return;
                }

                Log.Info(e.Data);
            };

            proc.ErrorDataReceived += (_, e) =>
            {
                if (string.IsNullOrEmpty(e.Data) || IsProgressLine(e.Data))
                {
                    return;
                }

                if (stdErrHandler is not null)
                {
                    stdErrHandler(e.Data);
                }
                else
                {
                    HandleStdErr(e.Data);
                }
            };

            proc.Start();
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

    public static ProcessResult RunAndCapture(string exe, ProcessArguments args)
    {
        try
        {
            using Process proc = CreateProcess(exe, args);

            StringBuilder output = new();
            StringBuilder error = new();

            proc.OutputDataReceived += (_, e) =>
            {
                if (e.Data is null)
                {
                    return;
                }

                output.AppendLine(e.Data);
            };
            proc.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is null)
                {
                    return;
                }

                error.AppendLine(e.Data);
            };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();

            return new(
                proc.ExitCode,
                output.ToString().Trim(),
                error.ToString().Trim());
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

    private static Process CreateProcess(string exe, ProcessArguments args)
    {
        Process proc = new()
        {
            StartInfo = new()
            {
                FileName = exe,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        foreach (string arg in args)
        {
            proc.StartInfo.ArgumentList.Add(arg);
        }

        return proc;
    }

    private static bool IsProgressLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        return (line.Contains("[download]", StringComparison.OrdinalIgnoreCase) && line.Contains('%'))
            || line.Contains("[frag]", StringComparison.OrdinalIgnoreCase)
            || (line.Contains("frame=", StringComparison.OrdinalIgnoreCase) && line.Contains("fps=", StringComparison.OrdinalIgnoreCase))
            || (line.Contains("size=", StringComparison.OrdinalIgnoreCase) && line.Contains("time=", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsNoiseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return true;
        }

        return line.StartsWith("[youtube]", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("[generic]", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("[info]", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("[download] Destination:", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("[ExtractAudio]", StringComparison.OrdinalIgnoreCase)
            || line.Contains("The url doesn't specify the protocol", StringComparison.OrdinalIgnoreCase);
    }

    private static void HandleStdErr(string data)
    {
        if (IsNoiseLine(data))
        {
            return;
        }

        if (data.Contains("DPAPI", StringComparison.OrdinalIgnoreCase))
        {
            Log.Error("Chrome cookie extraction failed due to a recent Google Chrome update.");
            Log.Error("Solution: Open 'Data/settings.toml', set CookiesBrowser = \"\", and use 'Cookies.txt' instead.");
            return;
        }

        if (data.Contains("Sign in to confirm", StringComparison.OrdinalIgnoreCase) ||
            (data.Contains("confirm", StringComparison.OrdinalIgnoreCase) && data.Contains("not a bot", StringComparison.OrdinalIgnoreCase)))
        {
            Log.Error("ERROR: YouTube bot verification requested. Please configure 'CookiesBrowser' or 'CookieFile' in 'Data/settings.toml'.");
            return;
        }

        if (data.Contains("error", StringComparison.OrdinalIgnoreCase) ||
            data.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
            data.Contains("fatal", StringComparison.OrdinalIgnoreCase))
        {
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
            Log.Warning(data);
        }
        else
        {
            Log.Info(data);
        }
    }
}