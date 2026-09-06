using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using SIASUN.RCS.Infrastructure.Logging.Banner;
using Shouldly;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Tests.Logging.Banner;

/// <summary>
/// 控制台启动横幅与配置渲染器单元测试
/// </summary>
public class RcsBannerRendererTests
{
    [Fact]
    public void Print_DefaultOptions_RendersSiasunSlantAndFooter()
    {
        // Arrange
        var options = new RcsBannerOptions();
        using var writer = new StringWriter();

        // Act
        RcsBannerRenderer.Print(options, writer);
        var output = writer.ToString();

        // Assert
        output.ShouldNotBeNullOrWhiteSpace();
        output.ShouldContain("SIASUN RCS");
        output.ShouldContain("新松机器人 | 移动机器人调度控制系统");
        output.ShouldContain(".NET 10 / ABP vNext");
    }

    [Fact]
    public void Print_AnsiShadowStyle_RendersBlockGraphics()
    {
        // Arrange
        var options = new RcsBannerOptions
        {
            Style = "AnsiShadow",
            ColorTheme = "CyanGradient"
        };
        using var writer = new StringWriter();

        // Act
        RcsBannerRenderer.Print(options, writer);
        var output = writer.ToString();

        // Assert
        output.ShouldContain("███████╗");
        output.ShouldContain("SIASUN RCS");
    }

    [Fact]
    public void Print_CompactStyle_RendersCompactLines()
    {
        // Arrange
        var options = new RcsBannerOptions
        {
            Style = "Compact"
        };
        using var writer = new StringWriter();

        // Act
        RcsBannerRenderer.Print(options, writer);
        var output = writer.ToString();

        // Assert
        output.ShouldContain("/ __|_ _|");
    }

    [Theory]
    [InlineData(false, "SiasunSlant")]
    [InlineData(true, "Off")]
    [InlineData(true, "off")]
    public void Print_WhenDisabledOrOff_OutputsNothing(bool enabled, string style)
    {
        // Arrange
        var options = new RcsBannerOptions
        {
            Enabled = enabled,
            Style = style
        };
        using var writer = new StringWriter();

        // Act
        RcsBannerRenderer.Print(options, writer);
        var output = writer.ToString();

        // Assert
        output.ShouldBeEmpty();
    }

    [Fact]
    public void Print_Monochrome_OutputsWithoutRgbEscapes()
    {
        // Arrange
        var options = new RcsBannerOptions
        {
            ColorTheme = "Monochrome"
        };
        using var writer = new StringWriter();

        // Act
        RcsBannerRenderer.Print(options, writer);
        var output = writer.ToString();

        // Assert
        output.ShouldNotContain("\x1b[38;2;");
        output.ShouldContain("SIASUN RCS");
    }

    [Fact]
    public void Print_FromConfiguration_BindsAndRendersConfiguredValues()
    {
        // Arrange
        var memoryConfig = new Dictionary<string, string?>
        {
            { "Banner:Enabled", "true" },
            { "Banner:Style", "AnsiShadow" },
            { "Banner:Title", "TEST SIASUN PLATFORM" },
            { "Banner:Subtitle", "半导体洁净室调度控制" },
            { "Banner:ColorTheme", "SiasunBlue" }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(memoryConfig)
            .Build();

        using var writer = new StringWriter();

        // Act
        var options = new RcsBannerOptions();
        config.GetSection(RcsBannerOptions.SectionName).Bind(options);
        RcsBannerRenderer.Print(options, writer);
        var output = writer.ToString();

        // Assert
        output.ShouldContain("TEST SIASUN PLATFORM");
        output.ShouldContain("半导体洁净室调度控制");
        output.ShouldContain("███████╗");
    }

    [Fact]
    public void Print_CustomFile_WhenFileExists_LoadsAndPrintsContent()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(tempFile, new[] { "=== CUSTOM BANNER LINE 1 ===", "=== CUSTOM BANNER LINE 2 ===" });

            var options = new RcsBannerOptions
            {
                CustomFile = tempFile
            };
            using var writer = new StringWriter();

            // Act
            RcsBannerRenderer.Print(options, writer);
            var output = writer.ToString();

            // Assert
            output.ShouldContain("=== CUSTOM BANNER LINE 1 ===");
            output.ShouldContain("=== CUSTOM BANNER LINE 2 ===");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}

