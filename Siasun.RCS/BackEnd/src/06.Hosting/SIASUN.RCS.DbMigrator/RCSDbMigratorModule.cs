using System.Diagnostics.CodeAnalysis;
using SIASUN.RCS.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace SIASUN.RCS.DbMigrator;

/// <summary>
/// SIASUN RCS 数据库自动迁移工具启动模块
/// </summary>
[DependsOn(
    typeof(AbpAutofacModule),
    typeof(RCSEntityFrameworkCoreModule),
    typeof(RCSApplicationContractsModule)
)]
[ExcludeFromCodeCoverage]
public class RCSDbMigratorModule : AbpModule
{
}
