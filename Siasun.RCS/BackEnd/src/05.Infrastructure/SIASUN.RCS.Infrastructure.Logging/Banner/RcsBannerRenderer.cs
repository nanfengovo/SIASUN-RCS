using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace SIASUN.RCS.Infrastructure.Logging.Banner;

/// <summary>
/// SIASUN RCS 工业级控制台启动横幅渲染器 (支持基于配置的风格与配色自适应渲染)
/// </summary>
public static class RcsBannerRenderer
{
    /// <summary>
    /// 从 IConfiguration 配置树读取 Banner 节并输出横幅至控制台
    /// </summary>
    /// <param name="configuration">应用配置根对象</param>
    public static void Print(IConfiguration? configuration)
    {
        var options = new RcsBannerOptions();
        configuration?.GetSection(RcsBannerOptions.SectionName).Bind(options);
        Print(options);
    }

    /// <summary>
    /// 根据配置选项将横幅渲染输出至指定输出流 (默认输出至 Console.Out)
    /// </summary>
    /// <param name="options">横幅选项配置</param>
    /// <param name="output">目标文本输出流，为空则使用 Console.Out</param>
    public static void Print(RcsBannerOptions? options, TextWriter? output = null)
    {
        options ??= new RcsBannerOptions();

        if (!options.Enabled || string.Equals(options.Style, "Off", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var writer = output ?? Console.Out;

        try
        {
            if (output == null)
            {
                Console.OutputEncoding = Encoding.UTF8;
            }

            // 1. 若指定了自定义文件且文件存在，优先加载文件内容
            if (!string.IsNullOrWhiteSpace(options.CustomFile) && File.Exists(options.CustomFile))
            {
                var customLines = File.ReadAllLines(options.CustomFile);
                RenderLines(writer, customLines, options.ColorTheme);
                RenderFooter(writer, options);
                return;
            }

            // 2. 根据 Style 选择艺术字
            var logoLines = options.Style?.ToLowerInvariant() switch
            {
                "ansishadow" or "block" => GetAnsiShadowLines(),
                "compact" or "small" => GetCompactLines(),
                _ => GetSiasunSlantLines() // 默认：匹配 logo.svg 斜体字样的 SiasunSlant
            };

            // 3. 按照配色方案输出艺术字
            RenderLines(writer, logoLines, options.ColorTheme);

            // 4. 输出工业平台元数据页脚
            RenderFooter(writer, options);
        }
        catch
        {
            // 终端环境异常时的降级保护
            writer.WriteLine($"\n=== {options.Title} ({options.Subtitle}) ===\n");
        }
    }

    /// <summary>
    /// 匹配 logo.svg 中 SIASUN 标志倾斜特征的前倾斜体 ASCII 艺术字 (5 行紧凑高辨识度)
    /// </summary>
    public static string[] GetSiasunSlantLines() => new[]
    {
        @"   _____ ____  ___   _____ __  ___   __     ____  ___________",
        @"  / ___//  _/ /   | / ___// / / / | / /    / __ \/ ____/ ___/",
        @"  \__ \ / /  / /| | \__ \/ / / /  |/ /    / /_/ / /    \__ \ ",
        @" ___/ // /  / ___ |___/ / /_/ / /|  /    / _, _/ /___ ___/ / ",
        @"/____/___/ /_/  |_/____/\____/_/ |_/    /_/ |_|\____//____/  "
    };

    /// <summary>
    /// 现代实心 3D 几何阴影块状艺术字 (6 行极高清晰度)
    /// </summary>
    public static string[] GetAnsiShadowLines() => new[]
    {
        "  ███████╗██╗ █████╗ ███████╗██╗   ██╗███╗   ██╗    ██████╗  ██████╗███████╗",
        "  ██╔════╝██║██╔══██╗██╔════╝██║   ██║████╗  ██║    ██╔══██╗██╔════╝██╔════╝",
        "  ███████╗██║███████║███████╗██║   ██║██╔██╗ ██║    ██████╔╝██║     ███████╗",
        "  ╚════██║██║██╔══██║╚════██║██║   ██║██║╚██╗██║    ██╔══██╗██║     ╚════██║",
        "  ███████║██║██║  ██║███████║╚██████╔╝██║ ╚████║    ██║  ██║╚██████╗███████║",
        "  ╚══════╝╚═╝╚═╝  ╚═╝╚══════╝ ╚═════╝ ╚═╝  ╚═══╝    ╚═╝  ╚═╝ ╚═════╝╚══════╝"
    };

    /// <summary>
    /// 极简 4 行紧凑艺术字
    /// </summary>
    public static string[] GetCompactLines() => new[]
    {
        @"  ___ ___   _   ___ _   _ _  _    ___  ___ ___ ",
        @" / __|_ _| /_\ / __| | | | \| |  | _ \/ __/ __|",
        @" \__ \| | / _ \\__ \ |_| | .` |  |   / (__\__ \",
        @" |___/___/_/ \_\___/\___/|_|\_|  |_|_\\___|___/"
    };

    /// <summary>
    /// 获取新松品牌色调渐变色阶 (参考 logo.svg 品牌主色 #005AFF 与 #033885)
    /// </summary>
    public static int[][] GetSiasunBluePalette(int lineCount)
    {
        var basePalette = new[]
        {
            new[] { 0, 215, 255 },  // 青色提亮
            new[] { 0, 160, 255 },  // 电光浅蓝
            new[] { 0, 90, 255 },   // logo.svg 新松官方电光蓝 (#005AFF)
            new[] { 10, 60, 180 },  // 钴蓝过渡
            new[] { 3, 56, 133 }    // logo.svg 新松官方深海蓝 (#033885)
        };

        return ResamplePalette(basePalette, lineCount);
    }

    /// <summary>
    /// 获取极光数码渐变色阶
    /// </summary>
    public static int[][] GetCyanGradientPalette(int lineCount)
    {
        var basePalette = new[]
        {
            new[] { 0, 240, 255 },
            new[] { 0, 205, 255 },
            new[] { 0, 170, 255 },
            new[] { 25, 135, 255 },
            new[] { 55, 105, 255 },
            new[] { 80, 85, 245 }
        };

        return ResamplePalette(basePalette, lineCount);
    }

    private static void RenderLines(TextWriter writer, string[] lines, string colorTheme)
    {
        writer.WriteLine();

        bool isMonochrome = string.Equals(colorTheme, "Monochrome", StringComparison.OrdinalIgnoreCase);
        int[][] palette = string.Equals(colorTheme, "CyanGradient", StringComparison.OrdinalIgnoreCase)
            ? GetCyanGradientPalette(lines.Length)
            : GetSiasunBluePalette(lines.Length);

        for (int i = 0; i < lines.Length; i++)
        {
            if (isMonochrome)
            {
                writer.WriteLine($"\x1b[1m{lines[i]}\x1b[0m");
            }
            else
            {
                var c = palette[Math.Min(i, palette.Length - 1)];
                writer.WriteLine($"\x1b[38;2;{c[0]};{c[1]};{c[2]}m\x1b[1m{lines[i]}\x1b[0m");
            }
        }

        writer.WriteLine();
    }

    private static void RenderFooter(TextWriter writer, RcsBannerOptions options)
    {
        bool isMonochrome = string.Equals(options.ColorTheme, "Monochrome", StringComparison.OrdinalIgnoreCase);

        if (isMonochrome)
        {
            writer.WriteLine($"  :: {options.Title} :: ({options.Subtitle})");
            writer.WriteLine("  :: Framework: .NET 10 / ABP vNext  :: Mode: Microkernel & Hexagonal Adapters");
            writer.WriteLine("  ────────────────────────────────────────────────────────────────────────────");
        }
        else
        {
            writer.WriteLine($"\x1b[38;2;0;160;255m\x1b[1m  :: {options.Title} ::\x1b[0m             \x1b[2m({options.Subtitle})\x1b[0m");
            writer.WriteLine("\x1b[2m  :: Framework: .NET 10 / ABP vNext             :: Mode: Microkernel & Hexagonal Adapters\x1b[0m");
            writer.WriteLine("\x1b[2m  ────────────────────────────────────────────────────────────────────────────\x1b[0m");
        }

        writer.WriteLine();
    }

    private static int[][] ResamplePalette(int[][] source, int targetCount)
    {
        if (targetCount <= 0) return Array.Empty<int[]>();
        if (targetCount == source.Length) return source;

        var result = new int[targetCount][];
        for (int i = 0; i < targetCount; i++)
        {
            float ratio = targetCount == 1 ? 0f : (float)i / (targetCount - 1);
            int idx = (int)Math.Round(ratio * (source.Length - 1));
            idx = Math.Clamp(idx, 0, source.Length - 1);
            result[i] = source[idx];
        }
        return result;
    }
}

