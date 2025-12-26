namespace MusicDownloader;

public static class ExecutableFinder
{
    public static string GetFullPath(string exeName, string configuredDir)
    {
        // 1. Prioritize the configured directory if it's provided.
        if (!string.IsNullOrWhiteSpace(configuredDir))
        {
            string pathFromConfig = Path.Combine(configuredDir, exeName);
            if (File.Exists(pathFromConfig))
            {
                return pathFromConfig;
            }
        }

        // 2. Search the system PATH environment variable.
        string? pathVar = Environment.GetEnvironmentVariable("PATH");
        if (pathVar != null)
        {
            foreach (string dir in pathVar.Split(Path.PathSeparator))
            {
                try
                {
                    string fullPath = Path.Combine(dir, exeName);
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }
                catch (ArgumentException)
                {
                    // Ignore invalid path characters in the PATH variable.
                }
            }
        }

        // 3. Fall back to the simple exe name and let the OS try to find it.
        // This will likely fail if the previous steps did, but it's the last resort.
        return exeName;
    }
}