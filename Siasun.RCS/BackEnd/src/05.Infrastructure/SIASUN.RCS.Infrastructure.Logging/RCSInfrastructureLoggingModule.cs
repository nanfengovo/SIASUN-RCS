using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IO;
using Volo.Abp.Modularity;

namespace SIASUN.RCS.Infrastructure.Logging
{
    /// <summary>
    /// SIASUN RCS 基础设施日志与全链路诊断模块
    /// </summary>
    [DependsOn(
        typeof(RCSDomainModule)
    )]
    [ExcludeFromCodeCoverage]
    public class RCSInfrastructureLoggingModule : AbpModule
    {
        /// <summary>
        /// 配置模块服务依赖与中台组件生命周期
        /// </summary>
        /// <param name="context">服务配置上下文</param>
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            // 注册特权审计证据策略（单例真实源）
            context.Services.AddSingleton<SIASUN.RCS.Diagnostics.IEvidencePrivilegePolicy>(SIASUN.RCS.Diagnostics.DefaultEvidencePrivilegePolicy.Instance);

            // 注册内存流池管理器（单例）
            context.Services.AddSingleton<RecyclableMemoryStreamManager>();

            // 注册 API 审计日志过滤评估器（单例内存快照）
            context.Services.AddSingleton<Filtering.IAuditLogFilterEvaluator, Filtering.AuditLogFilterEvaluator>();

            // 注册报文Channel(单例)
            context.Services.AddSingleton<ApiAuditLogChannel>();
            context.Services.AddSingleton<SIASUN.RCS.Auditing.IApiAuditLogChannel>(sp => sp.GetRequiredService<ApiAuditLogChannel>());

            // 注册实体日志 Channel(单例)
            context.Services.AddSingleton<SIASUN.RCS.Auditing.IEntityAuditLogChannel, EntityAuditLogChannel>();

            // 注册操作审计日志 Channel(单例)
            context.Services.AddSingleton<OperationLogs.OperationLogChannelManager>();
            context.Services.AddSingleton<SIASUN.RCS.Auditing.IOperationLogChannel>(sp => sp.GetRequiredService<OperationLogs.OperationLogChannelManager>());

            // 注册实体审计规则评估器（单例内存快照）
            context.Services.AddSingleton<SIASUN.RCS.Auditing.IEntityAuditRuleEvaluator, Filtering.EntityAuditRuleEvaluator>();

            // 注册后台批量写入Worker(HostedService)
            context.Services.AddHostedService<ApiAuditLogConsumer>();
            context.Services.AddHostedService<EntityAuditLogConsumer>();

            // 注册 SignalR 实时推流中台（支持规范 DiagnosticLiveStream 与 SignalRDiagnostics 双节配置驱动）
            var configuration = context.Services.GetConfiguration();
            context.Services.Configure<SIASUN.RCS.Diagnostics.DiagnosticLiveStreamOptions>(options =>
            {
                configuration.GetSection("SignalRDiagnostics").Bind(options);
                configuration.GetSection(SIASUN.RCS.Diagnostics.DiagnosticLiveStreamOptions.SectionName).Bind(options);
            });
            context.Services.Configure<Diagnostics.SignalR.SignalRDiagnosticsOptions>(options =>
            {
                configuration.GetSection("SignalRDiagnostics").Bind(options);
                configuration.GetSection(SIASUN.RCS.Diagnostics.DiagnosticLiveStreamOptions.SectionName).Bind(options);
            });
            context.Services.AddSignalR();
            context.Services.AddSingleton<Diagnostics.SignalR.IDiagnosticLiveStreamBroker, Diagnostics.SignalR.DiagnosticLiveStreamBroker>();
            context.Services.AddSingleton<SIASUN.RCS.Diagnostics.ILiveStreamTelemetryProvider>(sp => sp.GetRequiredService<Diagnostics.SignalR.IDiagnosticLiveStreamBroker>());
            context.Services.AddHostedService<Diagnostics.SignalR.DiagnosticLiveStreamWorker>();

            // 注册 AI 事故根因智能诊断引擎 (默认禁用，支持接入本地 Ollama / DeepSeek / 工业大模型)
            // 注册 AI 事故根因智能诊断引擎 (默认禁用，支持接入本地 Ollama / DeepSeek / 工业大模型，并内置确定性规则引擎无缝降级)
            context.Services.Configure<SIASUN.RCS.Diagnostics.AI.AiDiagnosticsOptions>(
                configuration.GetSection("AiDiagnostics"));
            context.Services.AddHttpClient("AiDiagnostics");
            context.Services.AddTransient<SIASUN.RCS.Diagnostics.AI.RuleBasedIncidentAnalysisProvider>();
            context.Services.AddTransient<SIASUN.RCS.Diagnostics.AI.IAiIncidentAnalysisProvider, Diagnostics.AI.OpenAiCompatibleAiIncidentAnalysisProvider>();
        }
    }
}
