using FluentAssertions;
using MusicDownloader.Commands;
using MusicDownloader.Common;
using MusicDownloader.Infrastructure;

namespace MusicDownloader.Tests.Commands;

public sealed class FfmpegCommandBuilderTests
{
    private readonly Settings _originalSettings;

    public FfmpegCommandBuilderTests()
    {
        _originalSettings = new()
        {
            AudioFormat = SettingsManager.Current.AudioFormat,
            AudioBitrateKbps = SettingsManager.Current.AudioBitrateKbps,
            PreservePitchWhenChangingTempo = SettingsManager.Current.PreservePitchWhenChangingTempo,
            FfmpegDir = SettingsManager.Current.FfmpegDir
        };
    }

    [Fact]
    public void Build_WithSimpleTrack_ReturnsCorrectCopyArguments()
    {
        // Arrange
        Track track = new()
        {
            Title = "Test Song",
            Artist = "Test Artist",
            Album = "Test Album",
            Loop = "1"
        };
        FfmpegCommandBuilder builder = new(track, "input.m4a", "output.m4a");

        // Act
        string command = builder.Build();

        // Assert
        command.Should().Contain("-i \"input.m4a\"");
        command.Should().Contain("-c:a copy");
        command.Should().Contain("-map 0:a");
        command.Should().Contain("-metadata title=\"Test Song\"");
        command.Should().Contain("-metadata artist=\"Test Artist\"");
        command.Should().Contain("-metadata album=\"Test Album\"");
        command.Should().Contain("\"output.m4a\"");
    }

    [Fact]
    public void Build_WithTrimAndNoLoop_AppendsSsAndTo()
    {
        // Arrange
        Track track = new()
        {
            Title = "Trimmed Song",
            Range = "00:10-01:20",
            Loop = "1"
        };
        FfmpegCommandBuilder builder = new(track, "input.m4a", "output.m4a");

        // Act
        string command = builder.Build();

        // Assert
        command.Should().Contain("-ss 00:10 -to 01:20");
        command.Should().Contain("-c:a copy");
    }

    [Fact]
    public void Build_WithLoopAndTrim_CreatesComplexFilterChain()
    {
        // Arrange
        Track track = new()
        {
            Title = "Looped Song",
            Range = "00:05-00:15",
            Loop = "3"
        };
        SettingsManager.Current.AudioBitrateKbps = 192;
        FfmpegCommandBuilder builder = new(track, "input.m4a", "output.m4a");

        // Act
        string command = builder.Build();

        // Assert
        command.Should().NotContain("-ss 00:05 -to 00:15 -i"); // trim is handled inside filter instead
        command.Should().Contain("-filter_complex \"[0:a]atrim=start=00:05:end=00:15,asetpts=PTS-STARTPTS,aloop=loop=2:size=2147483647[outa]\"");
        command.Should().Contain("-map \"[outa]\"");
        command.Should().Contain("-c:a aac -b:a 192k");

        // Clean up
        SettingsManager.Current.AudioBitrateKbps = _originalSettings.AudioBitrateKbps;
    }

    [Fact]
    public void Build_WithTempoChangeAndNoPitchPreservation_CalculatesSampleRateFilter()
    {
        // Arrange
        Track track = new()
        {
            Title = "Fast Song",
            Tempo = "110" // 10% faster
        };
        SettingsManager.Current.PreservePitchWhenChangingTempo = false;
        FfmpegCommandBuilder builder = new(track, "input.m4a", "output.m4a");

        // Act
        string command = builder.Build();

        // Assert
        command.Should().Contain("-filter:a \"asetrate=52800\"");
        command.Should().Contain("-c:a aac");
    }

    [Fact]
    public void Build_WithTempoChangeAndPitchPreservation_UsesAtempoFilter()
    {
        // Arrange
        Track track = new()
        {
            Title = "Fast Pitch Preserved Song",
            Tempo = "110"
        };
        SettingsManager.Current.PreservePitchWhenChangingTempo = true;
        FfmpegCommandBuilder builder = new(track, "input.m4a", "output.m4a");

        // Act
        string command = builder.Build();

        // Assert
        command.Should().Contain("-filter:a \"atempo=1.100\"");
        command.Should().Contain("-c:a aac");

        // Clean up
        SettingsManager.Current.PreservePitchWhenChangingTempo = _originalSettings.PreservePitchWhenChangingTempo;
    }

    [Fact]
    public void Build_WithCoverFile_MapsAttachedPicture()
    {
        // Arrange
        Track track = new() { Title = "Song With Art" };
        FfmpegCommandBuilder builder = new(track, "input.m4a", "output.m4a", "cover.jpg");

        // Act
        string command = builder.Build();

        // Assert
        command.Should().Contain("-i \"cover.jpg\"");
        command.Should().Contain("-map 1:0");
        command.Should().Contain("-c:v mjpeg -disposition:v:0 attached_pic");
    }
}