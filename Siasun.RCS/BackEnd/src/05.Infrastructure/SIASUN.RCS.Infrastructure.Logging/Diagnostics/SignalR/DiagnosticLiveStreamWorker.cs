using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SIASUN.RCS.Infrastructure.Logging.Diagnostics.SignalR
{
    public class DiagnosticLiveStreamWorker : BackgroundService
    {
        private readonly IDiagnosticLiveStreamBroker _broker;
        private readonly IHubContext<DiagnosticHub> _hubContext;
        private readonly SignalRDiagnosticsOptions _options;
        private readonly ILogger<DiagnosticLiveStreamWorker> _logger;

        public DiagnosticLiveStreamWorker(
            IDiagnosticLiveStreamBroker broker,
            IHubContext<DiagnosticHub> hubContext,
            IOptions<SignalRDiagnosticsOptions>? options,
            ILogger<DiagnosticLiveStreamWorker> logger)
        {
            _broker = broker;
            _hubContext = hubContext;
            _options = options?.Value ?? new SignalRDiagnosticsOptions();
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
