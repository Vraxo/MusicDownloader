using FluentAssertions;
using MusicDownloader.Commands;
using MusicDownloader.Common;
using MusicDownloader.Infrastructure;

namespace MusicDownloader.Tests.Commands;

public sealed class YtDlpCommandBuilderTests : IDisposable
{
    private readonly string? _originalCookiesBrowser;
    private readonly string _originalCookieFile;
    private readonly string _originalFfmpegDir;

    public YtDlpCommandBuilderTests()
    {
        _originalCookiesBrowser = SettingsManager.Current.CookiesBrowser;
        _originalCookieFile = SettingsManager.Current.CookieFile;
        _originalFfmpegDir = SettingsManager.Current.FfmpegDir;
    }

    public void Dispose()
    {
        SettingsManager.Current.CookiesBrowser = _originalCookiesBrowser;
        SettingsManager.Current.CookieFile = _originalCookieFile;
        SettingsManager.Current.FfmpegDir = _originalFfmpegDir;
    }

    [Fact]
    public void Build_WithValidTrack_GeneratesYtDlpCommand()
    {
        Track track = new()
        {
            Title = "Download Track",
            Source = "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
        };
        SettingsManager.Current.CookiesBrowser = "";
        SettingsManager.Current.CookieFile = "nonexistent_cookie_file.txt";
        SettingsManager.Current.FfmpegDir = "";

        YtDlpCommandBuilder builder = new(track, "temp_audio");

        string command = builder.Build();

        command.Should().Contain("-f \"bestaudio[ext=m4a]/bestaudio\"");
        command.Should().Contain("\"https://www.youtube.com/watch?v=dQw4w9WgXcQ\"");
        command.Should().Contain("--write-thumbnail");
        command.Should().Contain("-o \"temp_audio.%(ext)s\"");
        command.Should().NotContain("--cookies-from-browser");
        command.Should().NotContain("--ffmpeg-location");
    }

    [Fact]
    public void Build_WithBrowserCookiesAndFfmpegDir_IncludesFlags()
    {
        Track track = new()
        {
            Title = "Download Track",
            Source = "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
        };
        SettingsManager.Current.CookiesBrowser = "firefox";
        SettingsManager.Current.FfmpegDir = @"C:\FfmpegPath";

        YtDlpCommandBuilder builder = new(track, "temp_audio");

        string command = builder.Build();

        command.Should().Contain("--cookies-from-browser firefox");
        command.Should().Contain(@"--ffmpeg-location ""C:\FfmpegPath""");
    }
}