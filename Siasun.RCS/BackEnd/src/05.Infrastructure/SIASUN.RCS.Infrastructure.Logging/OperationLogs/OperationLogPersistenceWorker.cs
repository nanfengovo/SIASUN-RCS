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
    public class OperationLogPersistenceWorker : BackgroundService
    {
        private readonly OperationLogChannelManager _channelManager;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OperationLogPersistenceWorker> _logger;

        public OperationLogPersistenceWorker(
            OperationLogChannelManager channelManager,
            IServiceScopeFactory scopeFactory,
            ILogger<OperationLogPersistenceWorker> logger)
        {
            _channelManager = channelManager;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OperationLogPersistenceWorker started.");

            try
            {
                await foreach (var log in _channelManager.Channel.Reader.ReadAllAsync(stoppingToken))
                {
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
                        // 落盘失败（例如数据库挂了）不能抛出异常导致 Worker 停止，只能吞掉打本地 log
                        _logger.LogError(ex, "Failed to persist OperationLog for action: {Action}", log?.Action);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("OperationLogPersistenceWorker is stopping.");
            }
        }
    }
}
