using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Infrastructure.Logging.Diagnostics.SignalR;
using SIASUN.RCS.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace SIASUN.RCS.Infrastructure.Logging.Tasks
{
    /// <summary>
    /// 任务生命周期完结本地领域事件订阅处理器
    /// 严格遵循 SIASUN RCS 铁律 7：跨领域副作用（通知推流、实时看板广播等）统一由领域事件异步解耦处理
    /// </summary>
    public class TaskLifecycleEndedEventHandler : ILocalEventHandler<TaskLifecycleEndedEvent>, ITransientDependency
    {
        private readonly IDiagnosticLiveStreamBroker? _liveStreamBroker;
        private readonly ILogger<TaskLifecycleEndedEventHandler> _logger;

        /// <summary>
        /// 构造函数注入所需推流 Broker 与日志记录器
        /// </summary>
        /// <param name="logger">日志服务</param>
        /// <param name="liveStreamBroker">诊断推流 Broker（可选）</param>
        public TaskLifecycleEndedEventHandler(
            ILogger<TaskLifecycleEndedEventHandler> logger,
            IDiagnosticLiveStreamBroker? liveStreamBroker = null)
        {
            _logger = logger;
            _liveStreamBroker = liveStreamBroker;
        }

        /// <summary>
        /// 处理任务生命周期结束事件，异步广播至实时诊断与推流看板
        /// </summary>
        /// <param name="eventData">任务生命周期结束事件元数据</param>
        public Task HandleEventAsync(TaskLifecycleEndedEvent eventData)
        {
            if (eventData == null) return Task.CompletedTask;

            _logger.LogInformation(
                "Task [{TaskCode}] lifecycle ended with status [{Status}]. Reason: {Reason}",
                eventData.TaskCode,
                eventData.FinalStatus,
                eventData.Reason ?? "None");

            if (_liveStreamBroker != null && _liveStreamBroker.IsEnabled)
            {
                var isFailed = eventData.FinalStatus == AgvTaskStatus.Failed;
                var isCanceled = eventData.FinalStatus == AgvTaskStatus.Canceled;

                _liveStreamBroker.Publish(new LiveEventDto
                {
                    Timestamp = eventData.EndTime,
                    Track = "Task",
                    Level = isFailed ? "Error" : (isCanceled ? "Warning" : "Information"),
                    Source = "WorkflowEngine",
                    Title = $"任务 [{eventData.TaskCode}] 流程结束 ({eventData.FinalStatus})",
                    Summary = string.IsNullOrWhiteSpace(eventData.Reason)
                        ? $"任务 [{eventData.TaskCode}] 顺利完结"
                        : $"任务 [{eventData.TaskCode}] 结束，原因: {eventData.Reason}",
                    TraceId = eventData.TraceId,
                    TargetId = eventData.TaskId.ToString(),
                    VehicleId = eventData.AssignedVehicleCode
                });
            }

            return Task.CompletedTask;
        }
    }
}
