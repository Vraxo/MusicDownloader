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
            throw; // Re-throw to allow caller to handle the exception.
        }
    }
}