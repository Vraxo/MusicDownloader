namespace MusicDownloader.Common;

public static class UserInput
{
    private const string DataDir = "Data";

    public static string? GetInputFilePath()
    {
        if (!Directory.Exists(DataDir))
        {
            Log.Error($"Directory '{DataDir}' not found. Please create it and place your audio files inside.");
            return null;
        }

        List<string> files = [.. Directory.GetFiles(DataDir, "*.flac", SearchOption.TopDirectoryOnly)];

        if (files.Count == 0)
        {
            Log.Warning($"No .flac files found in the '{DataDir}' directory.");
            return null;
        }

        Log.Action("Please select a file to process:");

        for (int i = 0; i < files.Count; i++)
        {
            Console.WriteLine($"  {i + 1}: {Path.GetFileName(files[i])}");
        }

        Console.WriteLine();

        while (true)
        {
            Log.Action("Enter the number of the file:");

            string? input = Console.ReadLine()?.Trim();

            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= files.Count)
            {
                return files[choice - 1];
            }

            Log.Warning("Invalid selection. Please enter a number from the list.");
        }
    }

    public static string GetTempo()
    {
        Log.Action("Enter tempo change percentage (e.g., 110 for 10% faster, 95 for 5% slower). Leave empty for no change:");
        return Console.ReadLine()?.Trim() ?? "";
    }

    public static string GetTrimRange()
    {
        Log.Action("Enter trim range (e.g., 00:15-01:30 or 15-90). Leave empty for no trimming:");
        return Console.ReadLine()?.Trim() ?? "";
    }
}