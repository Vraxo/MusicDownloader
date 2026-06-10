using FluentAssertions;
using MusicDownloader.Common;

namespace MusicDownloader.Tests.Common;

public sealed class PathUtilsTests
{
    [Theory]
    [InlineData("normal_filename.txt", "normal_filename.txt")]
    [InlineData("file/name?with*invalid|chars.mp3", "file_name_with_invalid_chars.mp3")]
    [InlineData("album: special <edition> .m4a", "album_ special _edition_ .m4a")]
    [InlineData("", "")]
    public void SafeFileName_WithVariousInputs_SanitizesInvalidPathCharacters(string input, string expected)
    {
        // Act
        string result = PathUtils.SafeFileName(input);

        // Assert
        result.Should().Be(expected);
    }
}