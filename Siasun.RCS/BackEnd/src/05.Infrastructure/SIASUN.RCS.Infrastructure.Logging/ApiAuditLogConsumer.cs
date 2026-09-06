using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Auditing;

namespace SIASUN.RCS.Infrastructure.Logging
{
    /// <summary>
    /// 后台批量写入    
    /// </summary>
    public class ApiAuditLogConsumer : BackgroundService
    {

        private readonly ApiAuditLogChannel _channel;

        private readonly IApiAuditLogStore _store;

        private readonly ILogger<ApiAuditLogConsumer> _logger;
        private int _consecutiveFailureCount;

        public ApiAuditLogConsumer(ApiAuditLogChannel channel, IApiAuditLogStore store, ILogger<ApiAuditLogConsumer> logger)
        {
            _channel = channel;
            _store = store;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 启动时自愈回放磁盘未处理的特权溢出日志
            try
            {
                var recovered = _channel.RecoverDiskSpills();
                if (recovered > 0)
                {
                    _logger.LogWarning("ApiAuditLogConsumer 启动自愈成功从磁盘溢流恢复 {Count} 条特权 API 审计日志。", recovered);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ApiAuditLogConsumer 启动回放磁盘溢流日志失败: {Message}", ex.Message);
            }

            var batch = new List<ApiAuditLogEntry>(50);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 等待直到有数据可用（无超时限制，避免无意义的空转）
                    if (await _channel.Reader.WaitToReadAsync(stoppingToken))
                    {
                        // 一旦有第一条数据，开启 2 秒的攒批窗口
                        using var batchCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                        batchCts.CancelAfter(TimeSpan.FromSeconds(2));

                        try
                        {
                            while (batch.Count < 50 && !batchCts.IsCancellationRequested)
                            {
                                // 尝试同步读取，最多读取到 50 条
                                while (batch.Count < 50 && _channel.Reader.TryRead(out var entry))
                                {
                                    batch.Add(entry);
                                }

                                if (batch.Count >= 50)
                                {
                                    break; // 满 50 条，跳出攒批窗口
                                }

                                // 如果当前管道为空但没满 50 条，继续等待新数据（受 2 秒超时限制）
                                await _channel.Reader.WaitToReadAsync(batchCts.Token);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // 2 秒超时，正常吞下异常，继续往下执行落库
                        }

                        // 满 50 条或满 2 秒，执行批量落库
                        if (batch.Count > 0)
                        {
                            try
                            {
                                await _store.SaveBatchAsync(batch, stoppingToken);
                                _consecutiveFailureCount = 0;
                            }
                            catch (Exception saveEx)
                            {
                                _consecutiveFailureCount++;
                                _logger.LogError(saveEx, "异步批量持久化报文日志落库失败 (连续失败: {Count})，立即将批次中特权铁证回灌 SpillBuffer 应急落盘保全，坚决杜绝静默灭证", _consecutiveFailureCount);

                                // 回灌特权条目（状态码异常、未处理异常、特权端点），零静默丢失！
                                foreach (var item in batch)
                                {
                                    if (_channel.IsPrivilegedEntry(item))
                                    {
                                        _channel.SpillBuffer.Enqueue(item);
                                    }
                                }

                                throw; // 重新抛出触发外层 catch 的退避延时保护
                            }
                            finally
                            {
                                batch.Clear();
                            }
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _consecutiveFailureCount++;
                    _logger.LogError(ex, "异步批量持久化报文日志发生异常，进入退避等待 (连续失败: {Count})", _consecutiveFailureCount);
                    // 攒批或处理阶段异常：若 batch 中仍残余特权项（非 SaveBatch 抛出时的前置异常），在清空前回灌 SpillBuffer
                    foreach (var item in batch)
                    {
                        if (_channel.IsPrivilegedEntry(item))
                        {
                            _channel.SpillBuffer.Enqueue(item);
                        }
                    }
                    batch.Clear();
                    var delayMs = Math.Min(1000 * Math.Max(1, _consecutiveFailureCount), 5000);
                    await Task.Delay(delayMs, stoppingToken);
                }
            }
        }
    }
}