using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Logs.OperatorLogs;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace SIASUN.RCS.Infrastructure.Logging.OperationLogs
{
    /// <summary>
    /// 调度员操作与系统自审计日志后台持久化工作者（HostedService）
    /// 严格承载第 2 层不可抵赖铁证，支持启动时自愈回放本地磁盘溢流文件并优先持久化溢出环条目
    /// </summary>
    public class OperationLogPersistenceWorker : BackgroundService
    {
        private readonly OperationLogChannelManager _channelManager;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OperationLogPersistenceWorker> _logger;

        /// <summary>
        /// 构造函数注入通道管理器、作用域工厂与日志组件
        /// </summary>
        /// <param name="channelManager">操作审计通道管理器</param>
        /// <param name="scopeFactory">服务作用域工厂</param>
        /// <param name="logger">系统日志</param>
        public OperationLogPersistenceWorker(
            OperationLogChannelManager channelManager,
            IServiceScopeFactory scopeFactory,
            ILogger<OperationLogPersistenceWorker> logger)
        {
            _channelManager = channelManager;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OperationLogPersistenceWorker started.");

            // 启动时自愈回放本地磁盘未处理的特权操作审计溢出日志
            try
            {
                var recovered = _channelManager.RecoverDiskSpills();
                if (recovered > 0)
                {
                    _logger.LogWarning("OperationLogPersistenceWorker 启动自愈成功从磁盘溢流恢复 {Count} 条特权操作审计日志。", recovered);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OperationLogPersistenceWorker 启动回放磁盘溢流日志失败: {Message}", ex.Message);
            }

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    // 1. 优先排空紧急溢出环中的特权操作日志
                    while (_channelManager.SpillBuffer.TryDequeue(out var spillLog))
                    {
                        await PersistLogAsync(spillLog);
                    }

                    // 2. 等待常规通道可用
                    if (await _channelManager.Channel.Reader.WaitToReadAsync(stoppingToken))
                    {
                        while (_channelManager.Channel.Reader.TryRead(out var log))
                        {
                            await PersistLogAsync(log);

                            // 每次常规消费后，检查是否有新的紧急溢出日志
                            while (_channelManager.SpillBuffer.TryDequeue(out var newlySpilledLog))
                            {
                                await PersistLogAsync(newlySpilledLog);
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("OperationLogPersistenceWorker is stopping.");
            }
        }

        private async Task PersistLogAsync(OperationLog log)
        {
            if (log == null) return;

            try
            {
                // 获取全新独立的 Scope，完全脱离主 HTTP 请求上下文
                using var scope = _scopeFactory.CreateScope();

                // 开启独立的完整 UnitOfWork (RequiresNew)
                var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                using var uow = uowManager.Begin(new AbpUnitOfWorkOptions { IsTransactional = true }, requiresNew: true);

                var repository = scope.ServiceProvider.GetRequiredService<IRepository<OperationLog, Guid>>();
                await repository.InsertAsync(log);

                await uow.CompleteAsync();
            }
            catch (Exception ex)
            {
                // 落盘失败（例如数据库不可用）不能抛出异常导致 Worker 停止，打本地错误日志
                _logger.LogError(ex, "Failed to persist OperationLog for action: {Action}", log?.Action);
            }
        }
    }
}
