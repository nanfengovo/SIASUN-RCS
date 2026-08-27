using System.Diagnostics.CodeAnalysis;
using Volo.Abp.Account;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.TenantManagement;

namespace SIASUN.RCS;

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
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        RCSDtoExtensions.Configure();
    }
}
