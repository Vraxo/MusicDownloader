using System.ComponentModel;
using System.Diagnostics;

namespace MusicDownloader;

public class ProcessExecutor
{
    public static int Run(string exe, string args)
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

            proc.OutputDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) Log.Info(e.Data); };
            proc.ErrorDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) Log.Error(e.Data); };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();

            return proc.ExitCode;
        }
        catch (Win32Exception)
        {
            // Re-throw to be handled by the caller, e.g., for fallback logic.
            throw;
        }
        catch (Exception ex)
        {
            Log.Error($"Unexpected error running '{exe}': {ex.Message}");
            Environment.Exit(1);
            return -1;
        }
    }

    public static int RunWithFallback(string exe, string fallbackExe, string args)
    {
        try
        {
            return Run(exe, args);
        }
        catch (Win32Exception)
        {
            Log.Warning($"'{exe}' not found, trying '{fallbackExe}'...");

            try
            {
                return Run(fallbackExe, args);
            }
            catch (Win32Exception)
            {
                Log.Error($"Neither '{exe}' nor '{fallbackExe}' were found.");
                Environment.Exit(1);
                return -1;
            }
        }
    }
}