using System.Diagnostics.CodeAnalysis;
using Localization.Resources.AbpUi;
using SIASUN.RCS.Localization;
using Volo.Abp.Account;
using Volo.Abp.SettingManagement;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement.HttpApi;
using Volo.Abp.Localization;
using Volo.Abp.TenantManagement;

namespace SIASUN.RCS;

/// <summary>
/// SIASUN RCS Web API 控制器层模块
/// </summary>
[DependsOn(
    typeof(RCSApplicationContractsModule),
    typeof(AbpPermissionManagementHttpApiModule),
    typeof(AbpSettingManagementHttpApiModule),
    typeof(AbpAccountHttpApiModule),
    typeof(AbpIdentityHttpApiModule),
    typeof(AbpTenantManagementHttpApiModule),
    typeof(AbpFeatureManagementHttpApiModule)
    )]
[ExcludeFromCodeCoverage]
public class RCSHttpApiModule : AbpModule
{
    /// <summary>
    /// 配置多语言本地化资源
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ConfigureLocalization();
    }

    private void ConfigureLocalization()
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<RCSResource>()
                .AddBaseTypes(
                    typeof(AbpUiResource)
                );
        });
    }
}
