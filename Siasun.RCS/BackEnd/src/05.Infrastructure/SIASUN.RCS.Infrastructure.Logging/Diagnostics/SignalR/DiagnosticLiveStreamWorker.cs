using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SIASUN.RCS.Infrastructure.Logging.Diagnostics.SignalR
{
    /// <summary>
    /// SignalR 实时推流后台工作服务
    /// 遵循规范三.4节流要求，定时批量刷新待推流事件至客户端
    /// </summary>
    public class DiagnosticLiveStreamWorker : BackgroundService
    {
        private readonly IDiagnosticLiveStreamBroker _broker;
        private readonly IHubContext<DiagnosticHub> _hubContext;
        private readonly SIASUN.RCS.Diagnostics.DiagnosticLiveStreamOptions _options;
        private readonly ILogger<DiagnosticLiveStreamWorker> _logger;

        /// <summary>
        /// 构造函数注入所需依赖
        /// </summary>
        /// <param name="broker">实时诊断推流中台</param>
        /// <param name="hubContext">SignalR Hub 上下文</param>
        /// <param name="options">规范实时推流配置选项</param>
        /// <param name="logger">日志记录器</param>
        public DiagnosticLiveStreamWorker(
            IDiagnosticLiveStreamBroker broker,
            IHubContext<DiagnosticHub> hubContext,
            IOptions<SIASUN.RCS.Diagnostics.DiagnosticLiveStreamOptions>? options,
            ILogger<DiagnosticLiveStreamWorker> logger)
        {
            _broker = broker;
            _hubContext = hubContext;
            _options = options?.Value ?? new SIASUN.RCS.Diagnostics.DiagnosticLiveStreamOptions();
            _logger = logger;
        }



        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.IsEnabled)
            {
                _logger.LogInformation("SignalRDiagnostics is disabled. DiagnosticLiveStreamWorker is standing by.");
                return;
            }

            var interval = Math.Max(50, _options.FlushIntervalMs);
            _logger.LogInformation("SignalRDiagnostics live stream worker started. Flush interval: {Interval}ms", interval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(interval, stoppingToken);

                    var batches = _broker.DequeuePendingBatches();
                    if (batches.Count == 0) continue;

                    foreach (var (topic, events) in batches)
                    {
                        if (events.Count == 0) continue;
                        await _hubContext.Clients.Group(topic.ToLowerInvariant())
                            .SendAsync("ReceiveBatch", topic, events, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error occurred while flushing live diagnostic events to SignalR hub.");
                }
            }
        }
    }
}
