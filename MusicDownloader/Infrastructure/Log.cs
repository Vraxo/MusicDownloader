using Spectre.Console;

namespace MusicDownloader.Common;

internal sealed class Log
{
    public static void Info(string message)
    {
        WriteColored(message, Color.Grey);
    }

    public static void Warning(string message)
    {
        WriteColored(message, Color.Yellow);
    }

    public static void Error(string message)
    {
        WriteColored(message, Color.Red);
    }

    public static void Action(string message)
    {
        WriteColored(message, Color.Cyan);
    }

    public static void Success(string message)
    {
        WriteColored(message, Color.Green);
    }

    private static void WriteColored(string message, Color color)
    {
        AnsiConsole.Write(new Text(message, new(foreground: color)));
        AnsiConsole.WriteLine();
    }
}