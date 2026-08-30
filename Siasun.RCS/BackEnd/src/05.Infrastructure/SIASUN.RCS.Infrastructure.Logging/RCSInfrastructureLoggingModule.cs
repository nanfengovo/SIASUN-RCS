using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IO;
using Volo.Abp.Modularity;

namespace SIASUN.RCS.Infrastructure.Logging
{
    [DependsOn(
        typeof(RCSDomainModule)
    )]
    [ExcludeFromCodeCoverage]
    public class RCSInfrastructureLoggingModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            // 注册内存流池管理器（单例）
            context.Services.AddSingleton<RecyclableMemoryStreamManager>();

            // 注册 API 审计日志过滤评估器（单例内存快照）
            context.Services.AddSingleton<Filtering.IAuditLogFilterEvaluator, Filtering.AuditLogFilterEvaluator>();

            // 注册报文Channel(单例)
            context.Services.AddSingleton<ApiAuditLogChannel>();

            // 注册后台批量写入Worker(HostedService)
            context.Services.AddHostedService<ApiAuditLogConsumer>();
        }
    }
}