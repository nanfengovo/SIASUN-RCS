using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using SIASUN.RCS.Commands;
using Volo.Abp.Account;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace SIASUN.RCS;

/// <summary>
/// SIASUN RCS 应用服务层模块（业务用例、任务编排与监控服务）
/// </summary>
[DependsOn(
    typeof(RCSDomainModule),
    typeof(RCSApplicationContractsModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpAccountApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule)
    )]
[ExcludeFromCodeCoverage]
public class RCSApplicationModule : AbpModule
{

    /// <summary>
    /// 配置服务与依赖注入（注册 MediatR 与 CQRS 全局命令自动审计管道）
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(RCSApplicationModule).Assembly);
            cfg.AddOpenBehavior(typeof(CommandAuditPipelineBehavior<,>));
        });
    }
}
