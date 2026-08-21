using Volo.Abp.Modularity;

namespace SIASUN.RCS;

public abstract class RCSApplicationTestBase<TStartupModule> : RCSTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
