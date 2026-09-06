using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Account;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;
using Volo.Abp.VirtualFileSystem;

namespace SIASUN.RCS;

/// <summary>
/// SIASUN RCS 远程 HTTP API 客户端代理模块
/// </summary>
[DependsOn(
    typeof(RCSApplicationContractsModule),
    typeof(AbpPermissionManagementHttpApiClientModule),
    typeof(AbpFeatureManagementHttpApiClientModule),
    typeof(AbpAccountHttpApiClientModule),
    typeof(AbpIdentityHttpApiClientModule),
    typeof(AbpTenantManagementHttpApiClientModule),
    typeof(AbpSettingManagementHttpApiClientModule)
)]
[ExcludeFromCodeCoverage]
public class RCSHttpApiClientModule : AbpModule
{
    /// <summary>
    /// 默认远程服务名
    /// </summary>
    public const string RemoteServiceName = "Default";

    /// <summary>
    /// 配置动态 HTTP 客户端代理与嵌入式虚拟文件系统
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHttpClientProxies(
            typeof(RCSApplicationContractsModule).Assembly,
            RemoteServiceName
        );

        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<RCSHttpApiClientModule>();
        });
    }
}
