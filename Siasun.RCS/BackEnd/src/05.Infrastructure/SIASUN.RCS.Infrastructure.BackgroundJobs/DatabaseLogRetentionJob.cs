using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using SIASUN.RCS.Logs.OperatorLogs;
using SIASUN.RCS.Monitor;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace SIASUN.RCS.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// 主数据库日志定期归档与保留周期清理任务 (SQL Server / SQLite 双库适配)
    /// 自动清理超出保留天数的 OperationLog 与 SystemEventLog，保障工业数据库长期运行不爆库
    /// </summary>
    [DisallowConcurrentExecution]
    public class DatabaseLogRetentionJob : IJob
    {
        private readonly IRepository<OperationLog, Guid> _operationLogRepository;
        private readonly IRepository<SystemEventLog, Guid> _systemEventLogRepository;
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly ILogger<DatabaseLogRetentionJob> _logger;

        /// <summary>
        /// 构造函数注入操作与事件仓储
        /// </summary>
        public DatabaseLogRetentionJob(
            IRepository<OperationLog, Guid> operationLogRepository,
            IRepository<SystemEventLog, Guid> systemEventLogRepository,
            IConfiguration configuration,
            IUnitOfWorkManager unitOfWorkManager,
            ILogger<DatabaseLogRetentionJob> logger)
        {
            _operationLogRepository = operationLogRepository;
            _systemEventLogRepository = systemEventLogRepository;
            _configuration = configuration;
            _unitOfWorkManager = unitOfWorkManager;
            _logger = logger;
        }

        /// <summary>
        /// 执行主库历史日志清理工作流
        /// </summary>
        public async Task Execute(IJobExecutionContext context)
        {
            var retainDays = _configuration.GetValue<int>("DatabaseLogRetention:RetainDays", 90);
            if (retainDays <= 0)
            {
                _logger.LogInformation("DatabaseLogRetention:RetainDays 设置为 {RetainDays}，跳过主库日志清理", retainDays);
                return;
            }

            var thresholdTime = DateTime.UtcNow.AddDays(-retainDays);
            _logger.LogInformation("开始执行主数据库日志保留清理作业，截止时间 (UTC): {ThresholdTime}", thresholdTime);

            using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);

            try
            {
                // 1. 清理过期 OperationLog
                var opQuery = await _operationLogRepository.GetQueryableAsync();
                var expiredOps = opQuery.Where(x => x.CreationTime < thresholdTime).Take(2000).ToList();
                if (expiredOps.Count > 0)
                {
                    await _operationLogRepository.DeleteManyAsync(expiredOps, autoSave: true, cancellationToken: context.CancellationToken);
                    _logger.LogInformation("已清理 {Count} 条过期操作审计日志 (OperationLog)", expiredOps.Count);
                }

                // 2. 清理过期 SystemEventLog
                var sysQuery = await _systemEventLogRepository.GetQueryableAsync();
                var expiredSys = sysQuery.Where(x => x.CreationTime < thresholdTime).Take(2000).ToList();
                if (expiredSys.Count > 0)
                {
                    await _systemEventLogRepository.DeleteManyAsync(expiredSys, autoSave: true, cancellationToken: context.CancellationToken);
                    _logger.LogInformation("已清理 {Count} 条过期系统事件日志 (SystemEventLog)", expiredSys.Count);
                }

                await uow.CompleteAsync(context.CancellationToken);
                _logger.LogInformation("主数据库日志保留清理作业执行完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行主数据库日志保留清理作业时发生异常");
            }
        }
    }
}

