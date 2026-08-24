using SIASUN.RCS.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace SIASUN.RCS.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class RCSController : AbpControllerBase
{
    protected RCSController()
    {
        LocalizationResource = typeof(RCSResource);
    }
}
