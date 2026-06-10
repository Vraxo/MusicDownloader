using FluentAssertions;
using MusicDownloader.Common;

namespace MusicDownloader.Tests.Common;

public class TrackParserTests
{
    [Theory]
    [InlineData("00:15-01:30", "00:15", "01:30")]
    [InlineData("15-90", "15", "90")]
    public void TryParseRange_WithValidRange_ReturnsTrueAndOutputsStartAndEnd(string range, string expectedStart, string expectedEnd)
    {
        bool result = TrackParser.TryParseRange(range, out string start, out string end);

        result.Should().BeTrue();
        start.Should().Be(expectedStart);
        end.Should().Be(expectedEnd);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("00:15")]
    [InlineData("00:15-")]
    [InlineData("-01:30")]
    [InlineData("00:15-01:30-02:00")]
    public void TryParseRange_WithInvalidRange_ReturnsFalseAndOutputsEmptyStrings(string range)
    {
        bool result = TrackParser.TryParseRange(range, out string start, out string end);

        result.Should().BeFalse();
        start.Should().BeEmpty();
        end.Should().BeEmpty();
    }
}