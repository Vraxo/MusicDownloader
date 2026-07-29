using FluentAssertions;
using MusicDownloader.Stages.Storage;

namespace MusicDownloader.Tests.Stages.Storage;

public sealed class CsvTrackRepositoryTests
{
    [Fact]
    public void ParseCsvLine_WithQuotesAndCommas_CorrectlySplits()
    {
        string line = "value1,\"value,2\",\"value \"\"with\"\" quotes\",value4";
        List<string> result = CsvTrackRepository.ParseCsvLine(line);

        result.Should().HaveCount(4);
        result[0].Should().Be("value1");
        result[1].Should().Be("value,2");
        result[2].Should().Be("value \"with\" quotes");
        result[3].Should().Be("value4");
    }

    [Fact]
    public void FormatCsvValue_WithSpecialCharacters_WrapsAndEscapesQuotes()
    {
        CsvTrackRepository.FormatCsvValue("normal").Should().Be("normal");
        CsvTrackRepository.FormatCsvValue("with,comma").Should().Be("\"with,comma\"");
        CsvTrackRepository.FormatCsvValue("with\"quote").Should().Be("\"with\"\"quote\"");
        CsvTrackRepository.FormatCsvValue("with\nnewline").Should().Be("\"with\nnewline\"");
    }
}