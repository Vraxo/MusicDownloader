using FluentAssertions;
using MusicDownloader.Commands;
using MusicDownloader.Infrastructure;

namespace MusicDownloader.Tests.Commands;

public sealed class FlacFfmpegCommandBuilderTests
{
    private readonly Settings _originalSettings;

    public FlacFfmpegCommandBuilderTests()
    {
        _originalSettings = new()
        {
            PreservePitchWhenChangingTempo = SettingsManager.Current.PreservePitchWhenChangingTempo
        };
    }

    [Fact]
    public void Build_WithNoTempoAndNoRange_ReturnsCopyCommand()
    {
        // Arrange
        FlacFfmpegCommandBuilder builder = new(
            "input.flac",
            "output.flac",
            tempo: "",
            range: "",
            sampleRate: 44100);

        // Act
        string command = builder.Build();

        // Assert
        command.Should().Be("-y  -i \"input.flac\" -map 0 -map_metadata -1 -c copy \"output.flac\"");
    }

    [Fact]
    public void Build_WithTrimRange_AppendsTrimOptions()
    {
        // Arrange
        FlacFfmpegCommandBuilder builder = new(
            "input.flac",
            "output.flac",
            tempo: "",
            range: "00:10-00:20",
            sampleRate: 44100);

        // Act
        string command = builder.Build();

        // Assert
        command.Should().Contain("-ss 00:10 -to 00:20");
        command.Should().Contain("-c copy");
    }

    [Fact]
    public void Build_WithTempoChangeNoPitchPreserve_AppliesAsetrateFilter()
    {
        // Arrange
        SettingsManager.Current.PreservePitchWhenChangingTempo = false;
        FlacFfmpegCommandBuilder builder = new(
            "input.flac",
            "output.flac",
            tempo: "90",
            range: "",
            sampleRate: 44100);

        // Act
        string command = builder.Build();

        // Assert
        command.Should().Contain("-filter:a \"asetrate=39690\"");
        command.Should().Contain("-c:a flac");
    }

    [Fact]
    public void Build_WithTempoChangeAndPitchPreserve_AppliesAtempoFilter()
    {
        // Arrange
        SettingsManager.Current.PreservePitchWhenChangingTempo = true;
        FlacFfmpegCommandBuilder builder = new(
            "input.flac",
            "output.flac",
            tempo: "115",
            range: "",
            sampleRate: 48000);

        // Act
        string command = builder.Build();

        // Assert
        command.Should().Contain("-filter:a \"atempo=1.150\"");
        command.Should().Contain("-c:a flac");

        // Clean up
        SettingsManager.Current.PreservePitchWhenChangingTempo = _originalSettings.PreservePitchWhenChangingTempo;
    }
}