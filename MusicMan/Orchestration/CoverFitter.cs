using MusicMan.Core;
using MusicMan.Infrastructure;
using MusicMan.Stages.Processing;

namespace MusicMan.Orchestration;

/// <summary>
/// Re-shapes a downloaded thumbnail into a square cover image with ffmpeg.
/// Returns <c>null</c> whenever no work is needed or fitting fails, so callers can fall back
/// to the untouched download instead of losing the cover art.
/// </summary>
internal sealed class CoverFitter(CoverFitMode mode)
{
    private static readonly string[] FitExtensions = [".png", ".jpg", ".jpeg"];

    public async Task<string?> TryFitAsync(string sourcePath)
    {
        if (mode == CoverFitMode.Original)
        {
            return null;
        }

        // Only formats the fits are encoded back to are handled; anything else (e.g. WebP)
        // is stored as downloaded.
        string extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (!FitExtensions.Contains(extension))
        {
            return null;
        }

        string outputPath = Path.Combine(Path.GetTempPath(), $"musicman_cover_{Guid.NewGuid():N}{extension}");

        ProcessArguments command = CoverImageCommandBuilder.Build(
            mode,
            sourcePath,
            outputPath,
            SettingsManager.Current.CoverSize,
            SettingsManager.Current.CoverPadColor);

        string ffmpegPath = ExecutableFinder.GetFullPath(
            SettingsManager.Current.FfmpegExe,
            SettingsManager.Current.FfmpegDir);

        try
        {
            int exitCode = await Task.Run(() => ProcessExecutor.Run(ffmpegPath, command));
            if (exitCode == 0 && File.Exists(outputPath))
            {
                return outputPath;
            }

            Log.Warning($"Could not fit '{Path.GetFileName(sourcePath)}' ({mode}); keeping the original thumbnail.");
            DeleteQuietly(outputPath);
            return null;
        }
        catch (Exception ex)
        {
            Log.Warning($"Could not fit '{Path.GetFileName(sourcePath)}' ({mode}); keeping the original thumbnail. {ex.Message}");
            DeleteQuietly(outputPath);
            return null;
        }
    }

    public static void DeleteQuietly(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch { }
    }
}
