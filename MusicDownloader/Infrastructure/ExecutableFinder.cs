namespace MusicDownloader.Infrastructure;

internal static class ExecutableFinder
{
    public static string GetFullPath(string exeName, string configuredDir)
    {
        if (!string.IsNullOrWhiteSpace(configuredDir))
        {
            string pathFromConfig = Path.Combine(configuredDir, exeName);
            if (File.Exists(pathFromConfig))
            {
                return pathFromConfig;
            }
        }

        string? pathVar = Environment.GetEnvironmentVariable("PATH");
        if (pathVar is null)
        {
            return exeName;
        }

        char[] invalidChars = Path.GetInvalidPathChars();

        foreach (string dir in pathVar.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(dir) || dir.IndexOfAny(invalidChars) >= 0)
            {
                continue;
            }

            string fullPath = Path.Combine(dir, exeName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return exeName;
    }
}