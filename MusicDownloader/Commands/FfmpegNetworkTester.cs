using MusicDownloader.Common;
using MusicDownloader.Infrastructure;

namespace MusicDownloader.Commands;

internal static class FfmpegNetworkTester
{
    public static void Test()
    {
        Log.Info("Running FFmpeg network connectivity test (HTTPS)...");

        string ffmpegExe = ExecutableFinder.GetFullPath(SettingsManager.Current.FfmpegExe, SettingsManager.Current.FfmpegDir);
        string[] args = ["-v", "error", "-i", "https://www.google.com", "-f", "null", "-"];

        try
        {
            ProcessResult result = ProcessExecutor.RunAndCapture(ffmpegExe, args);
            string logs = result.StandardError;

            if (logs.Contains("Connection refused") ||
                logs.Contains("Network is unreachable") ||
                logs.Contains("I/O error") ||
                logs.Contains("TLS") ||
                logs.Contains("certificate"))
            {
                Log.Error("FFmpeg Connectivity: FAILED");
                Log.Error("FFmpeg cannot make HTTPS connections. This is likely a firewall or SSL certificate issue.");
                Log.Error("Raw Error: " + logs);
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