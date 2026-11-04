using MusicMan.Core;
using MusicMan.Infrastructure;

namespace MusicMan.Stages.Processing;

internal static class CoverImageCommandBuilder
{
    public static ProcessArguments Build(
        CoverFitMode mode,
        string inputFile,
        string outputFile,
        int size,
        string padColor)
    {
        List<string> args = ["-y", "-v", "error", "-i", inputFile];

        if (mode is CoverFitMode.SquareCrop or CoverFitMode.SquarePad)
        {
            args.AddRange(["-vf", BuildFilter(mode, size, padColor)]);
        }

        args.AddRange(["-frames:v", "1"]);

        if (outputFile.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            args.AddRange(["-pix_fmt", "rgb24", "-dpi", "300"]);
        }
        else if (outputFile.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
              || outputFile.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            args.AddRange(["-q:v", "2"]);
        }

        args.Add(outputFile);

        return args;
    }

    internal static string BuildFilter(CoverFitMode mode, int size, string padColor)
    {
        bool crop = mode == CoverFitMode.SquareCrop;
        List<string> steps = [];

        if (size > 0)
        {
            // Crop keeps one dimension at the square's edge; pad shrinks the whole frame
            // to fit inside the square and fills the rest with the background colour.
            steps.Add(crop
                ? "crop='min(iw,ih)':'min(iw,ih)'"
                : $"scale={size}:{size}:force_original_aspect_ratio=decrease:flags=lanczos");

            steps.Add(crop
                ? $"scale={size}:{size}:flags=lanczos"
                : $"pad={size}:{size}:(ow-iw)/2:(oh-ih)/2:color={NormalizeColor(padColor)}");
        }
        else
        {
            // No target size: square off at the source resolution instead of scaling up.
            steps.Add(crop
                ? "crop='min(iw,ih)':'min(iw,ih)'"
                : "scale='min(iw,ih)':-2:flags=lanczos");

            if (!crop)
            {
                steps.Add($"pad='max(iw,ih)':'max(iw,ih)':(ow-iw)/2:(oh-ih)/2:color={NormalizeColor(padColor)}");
            }
        }

        steps.Add("setsar=1");

        return string.Join(",", steps);
    }

    private static string NormalizeColor(string color)
    {
        return string.IsNullOrWhiteSpace(color) ? "0x101010" : color.Trim();
    }
}
