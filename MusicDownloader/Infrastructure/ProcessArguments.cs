using System.Collections;

namespace MusicDownloader.Infrastructure;

internal sealed class ProcessArguments(IEnumerable<string> args) : IReadOnlyList<string>
{
    private readonly List<string> _args = [.. args];

    public int Count => _args.Count;

    public string this[int index] => _args[index];

    public IEnumerator<string> GetEnumerator()
    {
        return _args.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public static implicit operator string(ProcessArguments p)
    {
        return string.Join(" ", p._args.Select(EscapeArgument));
    }

    public static implicit operator ProcessArguments(List<string> list)
    {
        return new(list);
    }

    public static implicit operator ProcessArguments(string[] array)
    {
        return new(array);
    }

    public override string ToString()
    {
        return this;
    }

    private static string EscapeArgument(string arg)
    {
        if (string.IsNullOrEmpty(arg))
        {
            return "\"\"";
        }

        int eqIndex = arg.IndexOf('=');
        if (eqIndex > 0)
        {
            string key = arg[..eqIndex];
            if (IsMetadataKey(key))
            {
                string val = arg[(eqIndex + 1)..];
                return $"{key}=\"{val.Replace("\"", "\\\"")}\"";
            }
        }

        return NeedsQuoting(arg)
            ? $"\"{arg.Replace("\"", "\\\"")}\""
            : arg;
    }

    private static bool NeedsQuoting(string arg)
    {
        return arg.Any(c => !char.IsLetterOrDigit(c) && c != '-' && c != '_' && c != ':');
    }

    private static bool IsMetadataKey(string key)
    {
        return key
            is "title"
            or "artist"
            or "album"
            or "track"
            or "disc"
            or "date"
            or "genre"
            or "comment";
    }
}