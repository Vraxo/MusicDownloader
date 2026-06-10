using FluentAssertions;
using MusicDownloader.Common;

namespace MusicDownloader.Tests.Common;

public sealed class TrackParserTests
{
    [Theory]
    [InlineData("00:15-01:30", "00:15", "01:30")]
    [InlineData(" 15 - 90 ", "15", "90")]
    [InlineData("0-100", "0", "100")]
    public void TryParseRange_WithValidRange_ReturnsTrueAndExtractsParts(string range, string expectedStart, string expectedEnd)
    {
        // Act
        bool result = TrackParser.TryParseRange(range, out string start, out string end);

        // Assert
        result.Should().BeTrue();
        start.Should().Be(expectedStart);
        end.Should().Be(expectedEnd);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("00:15")]
    [InlineData("00:15-01:30-02:00")]
    [InlineData("-01:30")]
    [InlineData("00:15-")]
    public void TryParseRange_WithInvalidRange_ReturnsFalseAndOutputsEmptyStrings(string? range)
    {
        // Act
        bool result = TrackParser.TryParseRange(range!, out string start, out string end);

        // Assert
        result.Should().BeFalse();
        start.Should().BeEmpty();
        end.Should().BeEmpty();
    }

    [Theory]
    [InlineData("100", 1.0)]
    [InlineData("110", 1.1)]
    [InlineData("95", 0.95)]
    [InlineData("125.5", 1.255)]
    public void TryParseTempo_WithValidNumericString_ReturnsTrueAndCalculatesMultiplier(string tempo, double expectedMultiplier)
    {
        // Act
        bool result = TrackParser.TryParseTempo(tempo, out double multiplier);

        // Assert
        result.Should().BeTrue();
        multiplier.Should().BeApproximately(expectedMultiplier, 1e-5);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("100%")]
    public void TryParseTempo_WithInvalidString_ReturnsFalseAndDefaultMultiplier(string? tempo)
    {
        // Act
        bool result = TrackParser.TryParseTempo(tempo!, out double multiplier);

        // Assert
        result.Should().BeFalse();
        multiplier.Should().Be(1.0);
    }
}