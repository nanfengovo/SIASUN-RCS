using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.SqlServer;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.BlobStoring.Database.EntityFrameworkCore;
using Volo.Abp.TenantManagement.EntityFrameworkCore;
using Volo.Abp.Studio;

namespace SIASUN.RCS.EntityFrameworkCore;

/// <summary>
/// SIASUN RCS Entity Framework Core 仓储与持久化模块（支持双数据库 SQL Server 与 SQLite 兼容）
/// </summary>
[DependsOn(
    typeof(RCSDomainModule),
    typeof(AbpPermissionManagementEntityFrameworkCoreModule),
    typeof(AbpSettingManagementEntityFrameworkCoreModule),
    typeof(AbpEntityFrameworkCoreSqlServerModule),
    typeof(AbpBackgroundJobsEntityFrameworkCoreModule),
    typeof(AbpAuditLoggingEntityFrameworkCoreModule),
    typeof(AbpFeatureManagementEntityFrameworkCoreModule),
    typeof(AbpIdentityEntityFrameworkCoreModule),
    typeof(AbpOpenIddictEntityFrameworkCoreModule),
    typeof(AbpTenantManagementEntityFrameworkCoreModule),
    typeof(BlobStoringDatabaseEntityFrameworkCoreModule)
    )]
[ExcludeFromCodeCoverage]
public class RCSEntityFrameworkCoreModule : AbpModule
{
    /// <summary>
    /// EF Core 预配置
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {

        RCSEfCoreEntityExtensionMappings.Configure();
    }

    /// <summary>
    /// 配置 DbContext 与实体审计拦截器
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<RCSDbContext>(options =>
        {
            /* Remove "includeAllEntities: true" to create
             * default repositories only for aggregate roots */
            options.AddDefaultRepositories(includeAllEntities: true);
        });

        if (AbpStudioAnalyzeHelper.IsInAnalyzeMode)
        {
            return;
        }

        Configure<AbpDbContextOptions>(options =>
        {
            options.PreConfigure<RCSDbContext>(ctx =>
            {
                var interceptor = ctx.ServiceProvider.GetRequiredService<SIASUN.RCS.EntityFrameworkCore.Auditing.EntityAuditInterceptor>();
                ctx.DbContextOptions.AddInterceptors(interceptor);
            });

            /* The main point to change your DBMS.
             * See also RCSDbContextFactory for EF Core tooling. */
            options.UseSqlServer();
        });

    }
}
