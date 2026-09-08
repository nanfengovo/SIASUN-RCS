using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Tasks.Profiling;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace SIASUN.RCS.Infrastructure.Logging.Profiling
{
    /// <summary>
    /// 任务步骤剖析后台批量落盘工作者
    /// 采用 2 秒时间窗口或满 50 条批量攒批写入，避免高频单条插入冲击数据库
    /// </summary>
    public class TaskProfilingConsumerWorker : BackgroundService
    {
        private readonly TaskProfilingChannel _channel;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TaskProfilingConsumerWorker> _logger;

        public TaskProfilingConsumerWorker(
            TaskProfilingChannel channel,
            IServiceScopeFactory scopeFactory,
            ILogger<TaskProfilingConsumerWorker> logger)
        {
            _channel = channel;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TaskProfilingConsumerWorker started.");

            var batch = new List<TaskStepProfiling>(50);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (await _channel.Reader.WaitToReadAsync(stoppingToken))
                    {
                        using var batchCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                        batchCts.CancelAfter(TimeSpan.FromSeconds(2));

                        try
                        {
                            while (batch.Count < 50 && await _channel.Reader.WaitToReadAsync(batchCts.Token))
                            {
                                if (_channel.Reader.TryRead(out var item))
                                {
                                    batch.Add(item);
                                }
                            }
                        }
                        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                        {
                            // 2 秒超时窗口到，正常结算当前批次
                        }

                        if (batch.Count > 0)
                        {
                            await FlushBatchAsync(batch);
                            batch.Clear();
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "TaskProfilingConsumerWorker 批处理循环发生未捕获异常: {Message}", ex.Message);
                    await Task.Delay(1000, stoppingToken);
                }
            }

            // 停机前尽量排空剩余条目
            try
            {
                while (_channel.Reader.TryRead(out var remaining))
                {
                    batch.Add(remaining);
                    if (batch.Count >= 50)
                    {
                        await FlushBatchAsync(batch);
                        batch.Clear();
                    }
                }
                if (batch.Count > 0)
                {
                    await FlushBatchAsync(batch);
                    batch.Clear();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("TaskProfilingConsumerWorker 停机排空剩余条目异常: {Message}", ex.Message);
            }
        }

        private async Task FlushBatchAsync(List<TaskStepProfiling> batch, CancellationToken cancellationToken = default)
        {
            const int maxRetries = 3;
            var attempt = 0;

            while (attempt < maxRetries)
            {
                attempt++;
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                    using var uow = uowManager.Begin(new AbpUnitOfWorkOptions { IsTransactional = true }, requiresNew: true);

                    var repository = scope.ServiceProvider.GetRequiredService<IRepository<TaskStepProfiling, Guid>>();
                    await repository.InsertManyAsync(batch, cancellationToken: cancellationToken);

                    await uow.CompleteAsync(cancellationToken);
                    return; // 批次持久化成功
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (attempt >= maxRetries)
                    {
                        _logger.LogError(ex, "批量持久化 {Count} 条任务步骤剖析记录重试 {Max} 次后最终失败: {Message}", batch.Count, maxRetries, ex.Message);
                    }
                    else
                    {
                        var backoffMs = attempt * 500;
                        _logger.LogWarning(ex, "批量持久化 {Count} 条任务步骤剖析记录失败 (第 {Attempt}/{Max} 次)，将在 {Delay}ms 后指数退避重试: {Message}",
                            batch.Count, attempt, maxRetries, backoffMs, ex.Message);
                        try
                        {
                            await Task.Delay(backoffMs, cancellationToken);
                        }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                        {
                            throw;
                        }
                    }
                }
            }
        }
    }
}
