using SIASUN.RCS.Localization;
using Volo.Abp.Application.Services;

namespace SIASUN.RCS;

/* Inherit your application services from this class.
 */
public abstract class RCSAppService : ApplicationService
{
    protected RCSAppService()
    {
        LocalizationResource = typeof(RCSResource);
    }
}
