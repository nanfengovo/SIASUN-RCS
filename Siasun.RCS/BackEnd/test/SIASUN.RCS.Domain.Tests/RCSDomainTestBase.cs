using Volo.Abp.Modularity;

namespace SIASUN.RCS;

/* Inherit from this class for your domain layer tests. */
public abstract class RCSDomainTestBase<TStartupModule> : RCSTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
