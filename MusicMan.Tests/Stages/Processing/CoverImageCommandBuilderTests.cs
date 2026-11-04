using FluentAssertions;
using MusicMan.Core;
using MusicMan.Infrastructure;
using MusicMan.Stages.Processing;

namespace MusicMan.Tests.Stages.Processing;

public sealed class CoverImageCommandBuilderTests
{
    [Fact]
    public void Build_WithOriginalMode_DoesNotReshapeImage()
    {
        ProcessArguments command = CoverImageCommandBuilder.Build(
            CoverFitMode.Original, "thumb.png", "cover.png", 640, "0x101010");

        string line = command.ToString();

        line.Should().NotContain("-vf");
        line.Should().Contain("-frames:v 1");
        line.Should().NotContain("-pix_fmt rgb24");
        line.Should().EndWith("cover.png");
    }

    [Fact]
    public void Build_WithSquareCrop_CropsCentreThenScales()
    {
        ProcessArguments command = CoverImageCommandBuilder.Build(
            CoverFitMode.SquareCrop, "thumb.png", "cover.png", 640, "0x101010");

        GetFilter(command).Should().Be(
            "crop='min(iw,ih)':'min(iw,ih)',scale=640:640:flags=lanczos,setsar=1");
    }

    [Fact]
    public void Build_WithSquarePad_ScalesToFitWithoutDistortingAndPads()
    {
        ProcessArguments command = CoverImageCommandBuilder.Build(
            CoverFitMode.SquarePad, "thumb.png", "cover.png", 512, "black");

        string filter = GetFilter(command);

        filter.Should().Be(
            "scale=512:512:force_original_aspect_ratio=decrease:flags=lanczos," +
            "pad=512:512:(ow-iw)/2:(oh-ih)/2:color=black,setsar=1");
        filter.Should().NotContain("crop=");
    }

    [Fact]
    public void Build_WithBlankPadColor_FallsBackToDefaultColor()
    {
        ProcessArguments command = CoverImageCommandBuilder.Build(
            CoverFitMode.SquarePad, "thumb.png", "cover.png", 640, "   ");

        GetFilter(command).Should().Contain("color=0x101010");
    }

    [Fact]
    public void Build_WithZeroSize_SquaresImageWithoutUpscaling()
    {
        string cropFilter = CoverImageCommandBuilder.BuildFilter(CoverFitMode.SquareCrop, 0, "black");
        string padFilter = CoverImageCommandBuilder.BuildFilter(CoverFitMode.SquarePad, 0, "black");

        cropFilter.Should().Be("crop='min(iw,ih)':'min(iw,ih)',setsar=1");
        padFilter.Should().StartWith("scale='min(iw,ih)':-2");
        padFilter.Should().Contain("pad='max(iw,ih)':'max(iw,ih)'");
    }

    [Fact]
    public void Build_WithPng_SetsDensityAndPixelFormat()
    {
        ProcessArguments command = CoverImageCommandBuilder.Build(
            CoverFitMode.SquareCrop, "thumb.png", "cover.PNG", 640, "0x101010");

        string line = command.ToString();

        line.Should().Contain("-pix_fmt rgb24");
        line.Should().Contain("-dpi 300");
        line.Should().NotContain("-q:v");
    }

    [Fact]
    public void Build_WithJpeg_UsesQualityButNoDensity()
    {
        ProcessArguments command = CoverImageCommandBuilder.Build(
            CoverFitMode.SquareCrop, "thumb.jpg", "cover.jpeg", 640, "0x101010");

        string line = command.ToString();

        line.Should().Contain("-q:v 2");
        line.Should().NotContain("-dpi");
    }

    private static string GetFilter(ProcessArguments command)
    {
        int index = command.ToList().IndexOf("-vf");
        index.Should().BeGreaterThanOrEqualTo(0);
        return command[index + 1];
    }
}
