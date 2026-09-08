using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Outbox.Events;
using Volo.Abp;
using Volo.Abp.EventBus.Local;

namespace SIASUN.RCS.Outbox
{
    /// <summary>
    /// SAGA 业务补偿协调器实现
    /// </summary>
    public class SagaCompensationCoordinator : ISagaCompensationCoordinator
    {
        private readonly IOutboxQueue _outboxQueue;
        private readonly ILocalEventBus _localEventBus;
        private readonly ILogger<SagaCompensationCoordinator> _logger;

        public SagaCompensationCoordinator(
            IOutboxQueue outboxQueue,
            ILocalEventBus localEventBus,
            ILogger<SagaCompensationCoordinator> logger)
        {
            _outboxQueue = outboxQueue;
            _localEventBus = localEventBus;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task TriggerCompensationAsync(
            Guid taskId,
            string taskCode,
            int failedStepIndex,
            string failureReason,
            string compensationAction,
            string? destination = null,
            string? traceId = null,
            CancellationToken cancellationToken = default)
        {
            Check.NotNullOrWhiteSpace(taskCode, nameof(taskCode));
            Check.NotNullOrWhiteSpace(compensationAction, nameof(compensationAction));

            _logger.LogWarning("触发任务 [{TaskCode}] 的 SAGA 补偿流程: [FailedStep={Step}, Action={Action}, Reason={Reason}]",
                taskCode, failedStepIndex, compensationAction, failureReason);

            // 1. 广播本地领域事件供内存监听器响应
            var compEvent = new SagaCompensationRequiredEvent(
                taskId,
                taskCode,
                failedStepIndex,
                failureReason,
                compensationAction,
                traceId);

            await _localEventBus.PublishAsync(compEvent);

            // 2. 将补偿通知写入 Outbox 事务表，通过 Polly 弹性重试上报外部系统（如通知 MES 撤销搬运运单）
            var compensationPayload = new
            {
                TaskId = taskId,
                TaskCode = taskCode,
                FailedStepIndex = failedStepIndex,
                FailureReason = failureReason,
                Action = compensationAction,
                TriggeredAt = DateTime.UtcNow
            };

            await _outboxQueue.EnqueueAsync(
                eventType: "SagaCompensationRequired",
                payload: compensationPayload,
                destination: destination ?? "MES",
                traceId: traceId,
                maxRetries: 5,
                cancellationToken: cancellationToken);
        }
    }
}
