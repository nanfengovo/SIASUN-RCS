using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Diagnostics;
using SIASUN.RCS.Infrastructure.Logging.Diagnostics.SignalR;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace SIASUN.RCS.Infrastructure.Logging.Diagnostics
{
    /// <summary>
    /// 磁盘自愈紧急清理触发领域事件订阅处理器
    /// 负责接收来自后台自愈 Job 的告警事件，并向 SignalR 实时看板广播顶级红色告警横幅（Banner）
    /// </summary>
    public class DiskSelfHealingTriggeredEventHandler : ILocalEventHandler<DiskSelfHealingTriggeredEvent>, ITransientDependency
    {
        private readonly IDiagnosticLiveStreamBroker? _liveStreamBroker;
        private readonly ILogger<DiskSelfHealingTriggeredEventHandler> _logger;

        /// <summary>
        /// 构造函数注入诊断推流 Broker 与日志记录器
        /// </summary>
        /// <param name="logger">日志服务</param>
        /// <param name="liveStreamBroker">诊断推流 Broker (可选)</param>
        public DiskSelfHealingTriggeredEventHandler(
            ILogger<DiskSelfHealingTriggeredEventHandler> logger,
            IDiagnosticLiveStreamBroker? liveStreamBroker = null)
        {
            _logger = logger;
            _liveStreamBroker = liveStreamBroker;
        }

        /// <summary>
        /// 处理磁盘自愈触发事件，向前端运维大屏与平板广播顶级红色警告
        /// </summary>
        /// <param name="eventData">自愈事件元数据</param>
        public Task HandleEventAsync(DiskSelfHealingTriggeredEvent eventData)
        {
            if (eventData == null) return Task.CompletedTask;

            _logger.LogCritical(
                "工控机磁盘自愈报警: 使用率达到 {PreUsagePercent}% (高水位线 {HighWatermark}%)。详情: {Message}",
                eventData.PreUsagePercent,
                eventData.HighWatermark,
                eventData.Message);

            if (_liveStreamBroker != null && _liveStreamBroker.IsEnabled)
            {
                _liveStreamBroker.Publish(new LiveEventDto
                {
                    Timestamp = eventData.Timestamp,
                    Track = DiagnosticTracks.Exception,
                    Level = DiagnosticLevels.Fatal,
                    Source = "DiskSelfHeal",
                    Title = $"【磁盘容量告警】使用率达到 {eventData.PreUsagePercent}% 触发紧急自愈清理",
                    Summary = string.IsNullOrWhiteSpace(eventData.Message)
                        ? $"磁盘空间达到高水位线 ({eventData.HighWatermark}%)，系统正在强制清理历史日志以防宕机"
                        : eventData.Message
                });
            }

            return Task.CompletedTask;
        }
    }
}

