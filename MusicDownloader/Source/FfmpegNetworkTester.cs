using System.Diagnostics;
using System.Text;

namespace MusicDownloader;

public static class FfmpegNetworkTester
{
    public static void Test()
    {
        Log.Info("Running FFmpeg network connectivity test (HTTPS)...");

        string ffmpegExe = ExecutableFinder.GetFullPath(SettingsManager.Current.FfmpegExe, SettingsManager.Current.FfmpegDir);

        // We test HTTPS specifically because the previous error was on port 443 (TCP/SSL).
        // google.com is a reliable target.
        string args = "-v error -i https://www.google.com -f null -";

        StringBuilder output = new();

        try
        {
            using Process proc = new()
            {
                StartInfo = new()
                {
                    FileName = ffmpegExe,
                    Arguments = args,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            proc.ErrorDataReceived += (_, e) => { if (e.Data != null) { _ = output.AppendLine(e.Data); } };
            _ = proc.Start();
            proc.BeginErrorReadLine();
            proc.WaitForExit();

            string logs = output.ToString();

            // Check for common network/SSL errors
            if (logs.Contains("Connection refused") ||
                logs.Contains("Network is unreachable") ||
                logs.Contains("I/O error") ||
                logs.Contains("TLS") ||
                logs.Contains("certificate"))
            {
                Log.Error("FFmpeg Connectivity: FAILED");
                Log.Error("FFmpeg cannot make HTTPS connections. This is likely a firewall or SSL certificate issue.");
                Log.Error("Raw Error: " + logs.Trim());
            }
            else
            {
                Log.Success("FFmpeg Connectivity: OK (HTTPS Connection successful)");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Could not run FFmpeg test: {ex.Message}");
        }
        Console.WriteLine();
    }
}