namespace MusicDownloader.Common;

internal sealed class Log
{
    private const ConsoleColor InfoColor = ConsoleColor.Gray;
    private const ConsoleColor WarningColor = ConsoleColor.Yellow;
    private const ConsoleColor ErrorColor = ConsoleColor.Red;
    private const ConsoleColor ActionColor = ConsoleColor.Cyan;
    private const ConsoleColor SuccessColor = ConsoleColor.Green;

    private static readonly Lock LockObj = new();

    public static void Info(string message)
    {
        Print(InfoColor, message);
    }

    public static void Warning(string message)
    {
        Print(WarningColor, message, isError: true);
    }

    public static void Error(string message)
    {
        Print(ErrorColor, message, isError: true);
    }

    public static void Action(string message)
    {
        Print(ActionColor, message);
    }

    public static void Success(string message)
    {
        Print(SuccessColor, message);
    }

    private static void Print(ConsoleColor color, string message, bool isError = false)
    {
        lock (LockObj)
        {
            Console.ForegroundColor = color;
            if (isError)
            {
                Console.Error.WriteLine(message);
            }
            else
            {
                Console.Out.WriteLine(message);
            }
            Console.ResetColor();
        }
    }
}