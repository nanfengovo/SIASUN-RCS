using System.Diagnostics.CodeAnalysis;
using Volo.Abp.Account;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.TenantManagement;

namespace SIASUN.RCS;

/// <summary>
/// SIASUN RCS 应用服务契约层模块（DTO、接口与权限常量定义）
/// </summary>
[DependsOn(
    typeof(RCSDomainSharedModule),
    typeof(AbpFeatureManagementApplicationContractsModule),
    typeof(AbpSettingManagementApplicationContractsModule),
    typeof(AbpIdentityApplicationContractsModule),
    typeof(AbpAccountApplicationContractsModule),
    typeof(AbpTenantManagementApplicationContractsModule),
    typeof(AbpPermissionManagementApplicationContractsModule)
)]
[ExcludeFromCodeCoverage]
public class RCSApplicationContractsModule : AbpModule
{
    /// <summary>
    /// 服务契约预配置
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        RCSDtoExtensions.Configure();
    }
}
