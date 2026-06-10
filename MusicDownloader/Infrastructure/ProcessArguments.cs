using System.Collections;

namespace MusicDownloader.Infrastructure;

internal sealed class ProcessArguments : IReadOnlyList<string>
{
    private readonly List<string> _args;

    public ProcessArguments(IEnumerable<string> args)
    {
        _args = [.. args];
    }

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
            string val = arg[(eqIndex + 1)..];
            if (val.Contains(' ') || val.Contains('"'))
            {
                return $"{key}=\"{val.Replace("\"", "\\\"")}\"";
            }
            return arg;
        }

        if (!arg.Contains(' ') && !arg.Contains('"'))
        {
            return arg;
        }

        return $"\"{arg.Replace("\"", "\\\"")}\"";
    }
}