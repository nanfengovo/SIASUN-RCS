using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using SIASUN.RCS.Outbox;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace SIASUN.RCS.Infrastructure.BackgroundJobs.Outbox
{
    /// <summary>
    /// 基于 Polly v8 弹性重试机制的 Outbox 可靠消息发布后台工作者
    /// 轮询 Pending 与重试就绪的 Outbox 消息，通过指数退避重试送达目标系统，达成事务最终一致性
    /// </summary>
    public class OutboxPublisherWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxPublisherWorker> _logger;
        private readonly ResiliencePipeline _resiliencePipeline;

        public OutboxPublisherWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<OutboxPublisherWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            // 基于 Polly v8 构建微秒级弹性重试策略（3 次快速重试 + 指数抖动退避）
            _resiliencePipeline = new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromMilliseconds(200),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true
                })
                .Build();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OutboxPublisherWorker 已启动，持续监控并弹性投递可靠事务消息...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessBatchAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "OutboxPublisherWorker 批次扫描异常: {Message}", ex.Message);
                }

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }

            _logger.LogInformation("OutboxPublisherWorker 已正常停止。");
        }

        /// <summary>
        /// 处理待发布的 Outbox 消息批次（公开此方法供单元测试调用验证）
        /// </summary>
        public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            using var uow = uowManager.Begin(requiresNew: true, isTransactional: true);

            var repository = scope.ServiceProvider.GetRequiredService<IRepository<OutboxMessage, Guid>>();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxMessageDispatcher>();

            var now = DateTime.UtcNow;
            var pendingMessages = await repository.GetListAsync(
                x => (x.Status == OutboxMessageStatus.Pending ||
                     (x.Status == OutboxMessageStatus.Publishing && x.NextRetryTime <= now)),
                cancellationToken: cancellationToken);

            if (pendingMessages.Count == 0)
            {
                await uow.CompleteAsync(cancellationToken);
                return 0;
            }

            var processedCount = 0;
            foreach (var message in pendingMessages.Take(20))
            {
                try
                {
                    message.MarkAsPublishing();

                    // 使用 Polly 弹性管道执行外部网络投递
                    await _resiliencePipeline.ExecuteAsync(async state =>
                    {
                        await dispatcher.DispatchAsync(message, cancellationToken);
                    }, cancellationToken);

                    message.MarkAsPublished();
                    processedCount++;
                }
                catch (Exception ex)
                {
                    var backoffDelay = TimeSpan.FromSeconds(Math.Pow(2, message.RetryCount + 1));
                    message.RecordRetryFailure(ex.Message, backoffDelay);
                    _logger.LogWarning(ex, "Outbox 消息 [{Id}] 投递失败 (重试 {RetryCount}/{MaxRetries}): {Message}",
                        message.Id, message.RetryCount, message.MaxRetries, ex.Message);
                }

                await repository.UpdateAsync(message, autoSave: true, cancellationToken: cancellationToken);
            }

            await uow.CompleteAsync(cancellationToken);
            return processedCount;
        }
    }
}
