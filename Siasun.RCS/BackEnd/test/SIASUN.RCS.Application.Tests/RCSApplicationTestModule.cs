using Volo.Abp.Modularity;

namespace SIASUN.RCS;

[DependsOn(
    typeof(RCSApplicationModule),
    typeof(RCSDomainTestModule)
)]
public class RCSApplicationTestModule : AbpModule
{

}
