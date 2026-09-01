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

        public ApiAuditLogConsumer(ApiAuditLogChannel channel, IApiAuditLogStore store, ILogger<ApiAuditLogConsumer> logger)
        {
            _channel = channel;
            _store = store;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
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
                            await _store.SaveBatchAsync(batch, stoppingToken);
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
                    _logger.LogError(ex, "异步批量持久化报文日志发生异常");
                    // 发生异常时清空批次，避免一直重复引发错误
                    batch.Clear();
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}