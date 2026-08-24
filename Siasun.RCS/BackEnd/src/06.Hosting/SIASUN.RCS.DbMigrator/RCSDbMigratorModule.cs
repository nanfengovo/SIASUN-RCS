using SIASUN.RCS.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace SIASUN.RCS.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(RCSEntityFrameworkCoreModule),
    typeof(RCSApplicationContractsModule)
)]
public class RCSDbMigratorModule : AbpModule
{
}
