namespace MusicDownloader.Infrastructure;

public record ProcessResult(int ExitCode, string StandardOutput, string StandardError);