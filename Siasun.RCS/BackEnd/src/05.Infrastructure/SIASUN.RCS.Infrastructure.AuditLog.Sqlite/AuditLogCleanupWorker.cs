using System.Threading.Tasks;
using Quartz;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Infrastructure.AuditLog.Sqlite
{
    /// <summary>
    /// Quartz Job：审计日志清理调度适配器
    /// 本类只是调度框架的"触发器桩"，业务逻辑全部委托给 <see cref="AuditLogCleanupService"/>。
    /// 调度注册由 SIASUN.RCS.Infrastructure.BackgroundJobs 统一管理，本模块无 Quartz 模块依赖。
    /// </summary>
    [DisallowConcurrentExecution]
    public class AuditLogCleanupJob : IJob, ITransientDependency
    {
        private readonly AuditLogCleanupService _cleanupService;

        /// <summary>
        /// 构造函数注入审计日志清理服务
        /// </summary>
        /// <param name="cleanupService">审计日志清理服务</param>
        public AuditLogCleanupJob(AuditLogCleanupService cleanupService)
        {
            _cleanupService = cleanupService;
        }

        /// <summary>
        /// 执行定期分片文件清理作业
        /// </summary>
        /// <param name="context">作业执行上下文</param>
        public Task Execute(IJobExecutionContext context)
        {
            using var traceScope = SIASUN.RCS.Diagnostics.RcsTraceContext.SetScoped($"JOB-SQLITECLEANUP-{System.Guid.NewGuid():N}");
            return _cleanupService.CleanExpiredFilesAsync(context.CancellationToken);
        }
    }
}
