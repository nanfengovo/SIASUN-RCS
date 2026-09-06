using Microsoft.Extensions.Localization;
using SIASUN.RCS.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace SIASUN.RCS;

/// <summary>
/// SIASUN RCS 应用程序品牌标识提供者
/// </summary>
[Dependency(ReplaceServices = true)]
public class RCSBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<RCSResource> _localizer;

    /// <summary>
    /// 初始化品牌提供者
    /// </summary>
    /// <param name="localizer">本地化器</param>
    public RCSBrandingProvider(IStringLocalizer<RCSResource> localizer)
    {
        _localizer = localizer;
    }

    /// <summary>
    /// 应用程序显示名称
    /// </summary>
    public override string AppName => _localizer["AppName"];
}
