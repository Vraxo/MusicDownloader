namespace MusicDownloader.Infrastructure;

internal record ProcessResult(int ExitCode, string StandardOutput, string StandardError);