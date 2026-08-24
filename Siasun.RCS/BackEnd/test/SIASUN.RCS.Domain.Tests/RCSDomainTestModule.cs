using Volo.Abp.Modularity;

namespace SIASUN.RCS;

[DependsOn(
    typeof(RCSDomainModule),
    typeof(RCSTestBaseModule)
)]
public class RCSDomainTestModule : AbpModule
{

}
