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
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Log.Info(e.Data);
                }
            };

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

            return new(proc.ExitCode, output.ToString().Trim(), error.ToString().Trim());
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

    private static void HandleStdErr(string data)
    {
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