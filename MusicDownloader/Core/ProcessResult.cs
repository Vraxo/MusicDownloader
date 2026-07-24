namespace MusicDownloader.Core;

internal record ProcessResult(int ExitCode, string StandardOutput, string StandardError);