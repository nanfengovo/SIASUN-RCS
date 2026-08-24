using Microsoft.Extensions.Localization;
using SIASUN.RCS.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace SIASUN.RCS;

[Dependency(ReplaceServices = true)]
public class RCSBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<RCSResource> _localizer;

    public RCSBrandingProvider(IStringLocalizer<RCSResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
