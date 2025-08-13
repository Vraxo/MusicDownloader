namespace MusicDownloader;

public class Log
{
    private const ConsoleColor InfoColor = ConsoleColor.Gray;
    private const ConsoleColor WarningColor = ConsoleColor.Yellow;
    private const ConsoleColor ErrorColor = ConsoleColor.Red;
    private const ConsoleColor ActionColor = ConsoleColor.Cyan;
    private const ConsoleColor SuccessColor = ConsoleColor.Green;

    public static void Info(string message)
    {
        Print(InfoColor, message);
    }

    public static void Warning(string message)
    {
        Print(WarningColor, message);
    }

    public static void Error(string message)
    {
        Print(ErrorColor, message);
    }

    public static void Action(string message)
    {
        Print(ActionColor, message);
    }

    public static void Success(string message)
    {
        Print(SuccessColor, message);
    }

    private static void Print(ConsoleColor color, string message)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}