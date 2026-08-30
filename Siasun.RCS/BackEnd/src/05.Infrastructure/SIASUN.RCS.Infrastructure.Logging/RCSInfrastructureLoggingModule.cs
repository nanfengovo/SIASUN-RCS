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
            
            // 注册实体日志 Channel(单例)
            context.Services.AddSingleton<SIASUN.RCS.Auditing.IEntityAuditLogChannel, EntityAuditLogChannel>();

            // 注册实体审计规则评估器（单例内存快照）
            context.Services.AddSingleton<SIASUN.RCS.Auditing.IEntityAuditRuleEvaluator, Filtering.EntityAuditRuleEvaluator>();

            // 注册后台批量写入Worker(HostedService)
            context.Services.AddHostedService<ApiAuditLogConsumer>();
            context.Services.AddHostedService<EntityAuditLogConsumer>();
        }
    }
}
