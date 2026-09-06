using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Diagnostics;
using SIASUN.RCS.Hardware;
using SIASUN.RCS.Infrastructure.Logging.Diagnostics.SignalR;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace SIASUN.RCS.Infrastructure.Logging.Hardware
{
    /// <summary>
    /// PLC 信号跳变（Edge Trigger）变位领域事件订阅处理器
    /// 遵循《AGENTS.md》铁律 4 与铁律 7：工控硬件交互由领域事件解耦，接收硬件适配器跳变信号并广播至实时诊断与时序看板
    /// </summary>
    public class PlcSignalChangedEventHandler : ILocalEventHandler<PlcSignalChangedEvent>, ITransientDependency
    {
        private readonly IDiagnosticLiveStreamBroker? _liveStreamBroker;
        private readonly ILogger<PlcSignalChangedEventHandler> _logger;

        /// <summary>
        /// 构造函数注入诊断推流 Broker 与日志服务
        /// </summary>
        /// <param name="logger">日志服务</param>
        /// <param name="liveStreamBroker">诊断推流 Broker (可选)</param>
        public PlcSignalChangedEventHandler(
            ILogger<PlcSignalChangedEventHandler> logger,
            IDiagnosticLiveStreamBroker? liveStreamBroker = null)
        {
            _logger = logger;
            _liveStreamBroker = liveStreamBroker;
        }

        /// <summary>
        /// 异步处理 PLC 信号跳变变位领域事件并广播至 HardwareGate 诊断泳道
        /// </summary>
        /// <param name="eventData">PLC 变位事件数据</param>
        public Task HandleEventAsync(PlcSignalChangedEvent eventData)
        {
            if (eventData == null) return Task.CompletedTask;

            var isAlarm = IsAlarmSignal(eventData.TagName, eventData.NewValue);
            var logLevel = isAlarm ? LogLevel.Warning : LogLevel.Information;

            _logger.Log(
                logLevel,
                "PLC 硬件信号变位: 设备 [{DeviceId}], 点位 [{TagName}], 值变化: [{OldValue}] -> [{NewValue}], 数据类型: [{DataType}]",
                eventData.DeviceId,
                eventData.TagName,
                eventData.OldValue ?? "null",
                eventData.NewValue ?? "null",
                eventData.DataType ?? "N/A");

            if (_liveStreamBroker != null && _liveStreamBroker.IsEnabled)
            {
                var diagLevel = isAlarm ? DiagnosticLevels.Warning : DiagnosticLevels.Information;

                _liveStreamBroker.Publish(new LiveEventDto
                {
                    Timestamp = eventData.Timestamp,
                    Track = DiagnosticTracks.HardwareGate,
                    Level = diagLevel,
                    Source = string.IsNullOrWhiteSpace(eventData.DeviceId) ? "PLC" : eventData.DeviceId,
                    Title = $"PLC 信号变位: {eventData.DeviceId}.{eventData.TagName}",
                    Summary = $"值变化: [{eventData.OldValue}] -> [{eventData.NewValue}]",
                    TraceId = eventData.TraceId,
                    VehicleId = eventData.VehicleCode,
                    TargetId = eventData.TaskCode
                });
            }

            return Task.CompletedTask;
        }

        private static bool IsAlarmSignal(string tagName, object? newValue)
        {
            if (string.IsNullOrWhiteSpace(tagName)) return false;

            if (tagName.Contains("alarm", StringComparison.OrdinalIgnoreCase) ||
                tagName.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                tagName.Contains("fault", StringComparison.OrdinalIgnoreCase) ||
                tagName.Contains("estop", StringComparison.OrdinalIgnoreCase) ||
                tagName.Contains("warning", StringComparison.OrdinalIgnoreCase))
            {
                if (newValue is bool b && b) return true;
                if (newValue is int i && i != 0) return true;
                return true;
            }

            return false;
        }
    }
}

