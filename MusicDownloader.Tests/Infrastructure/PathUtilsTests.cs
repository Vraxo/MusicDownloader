using FluentAssertions;
using MusicDownloader.Core;
using MusicDownloader.Infrastructure;

namespace MusicDownloader.Tests.Infrastructure;

public sealed class PathUtilsTests
{
    [Theory]
    [InlineData("normal_filename.txt", "normal_filename.txt")]
    [InlineData("file/name?with*invalid|chars.mp3", "file_name_with_invalid_chars.mp3")]
    [InlineData("album: special <edition> .m4a", "album_ special _edition_ .m4a")]
    [InlineData("", "")]
    public void SafeFileName_WithVariousInputs_SanitizesInvalidPathCharacters(string input, string expected)
    {
        string result = PathUtils.SafeFileName(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void GetCoverFileName_WithEmptyCover_ReturnsTitlePng()
    {
        Track track = new() { Title = "Song Title?" };
        string result = PathUtils.GetCoverFileName(track);
        result.Should().Be("Song Title_.png");
    }

    [Theory]
    [InlineData("[[Covers/album-art.jpg]]", "album-art.jpg")]
    [InlineData("[[Covers\\album-art.png]]", "album-art.png")]
    [InlineData("plain-file.webp", "plain-file.webp")]
    public void GetCoverFileName_WithCover_ExtractsFileName(string cover, string expected)
    {
        Track track = new() { Title = "Song", Cover = cover };
        string result = PathUtils.GetCoverFileName(track);
        result.Should().Be(expected);
    }
}