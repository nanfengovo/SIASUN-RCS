using System.Diagnostics.CodeAnalysis;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.Account;
using Volo.Abp.Identity;
using Volo.Abp.Mapperly;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection;
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

}
