namespace SIASUN.RCS.Infrastructure.Logging.Banner;

/// <summary>
/// 控制台启动横幅与品牌标识配置选项
/// </summary>
public class RcsBannerOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "Banner";

    /// <summary>
    /// 是否在应用启动时打印控制台横幅，默认 true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 横幅艺术字风格:
    /// - "SiasunSlant": 匹配 logo.svg 中 SIASUN 品牌英文的动态前倾斜体风格 (默认)
    /// - "AnsiShadow": 现代高辨识度 3D 几何实心块状阴影风格
    /// - "Compact": 极简紧凑 4 行低矮风格
    /// - "Custom": 从外部文件加载自定义 ASCII 艺术字
    /// - "Off": 完全关闭横幅打印
    /// </summary>
    public string Style { get; set; } = "SiasunSlant";

    /// <summary>
    /// 品牌主标题 (默认 "SIASUN RCS")
    /// </summary>
    public string Title { get; set; } = "SIASUN RCS";

    /// <summary>
    /// 品牌副标题或业务定位描述 (参考 logo.svg "SIASUN 新松": "新松机器人 | 移动机器人调度控制系统")
    /// </summary>
    public string Subtitle { get; set; } = "新松机器人 | 移动机器人调度控制系统";

    /// <summary>
    /// 终端配色方案:
    /// - "SiasunBlue": 源自 logo.svg 的新松品牌专属色谱渐变 (#005AFF 电光蓝 与 #033885 深海蓝) (默认)
    /// - "CyanGradient": 极光青至深科技蓝现代数码渐变
    /// - "Monochrome": 纯色终端兼容模式 (无 RGB 颜色转义序列)
    /// </summary>
    public string ColorTheme { get; set; } = "SiasunBlue";

    /// <summary>
    /// 自定义横幅文件路径 (例如 "banner.txt"，若指定且文件存在则优先读取该文件内容)
    /// </summary>
    public string? CustomFile { get; set; }
}

