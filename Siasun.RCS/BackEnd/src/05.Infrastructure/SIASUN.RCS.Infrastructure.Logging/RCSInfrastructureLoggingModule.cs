using Microsoft.Extensions.DependencyInjection;
using Microsoft.IO;
using Volo.Abp.Modularity;

namespace SIASUN.RCS.Infrastructure.Logging
{
    [DependsOn(
        typeof(RCSDomainModule)
    )]
    public class RCSInfrastructureLoggingModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            // 注册内存流池管理器（单例）
            context.Services.AddSingleton<RecyclableMemoryStreamManager>();

            // 注册报文Channel(单例)
            context.Services.AddSingleton<ApiAuditLogChannel>();

            // 注册后台批量写入Worker(HostedService)
            context.Services.AddHostedService<ApiAuditLogConsumer>();
        }
    }
}