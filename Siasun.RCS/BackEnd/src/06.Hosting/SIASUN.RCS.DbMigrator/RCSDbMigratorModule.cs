using System.Diagnostics.CodeAnalysis;
using SIASUN.RCS.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace SIASUN.RCS.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(RCSEntityFrameworkCoreModule),
    typeof(RCSApplicationContractsModule)
)]
[ExcludeFromCodeCoverage]
public class RCSDbMigratorModule : AbpModule
{
}
