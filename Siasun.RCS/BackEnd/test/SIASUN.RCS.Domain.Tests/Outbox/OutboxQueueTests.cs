using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Outbox;
using SIASUN.RCS.Outbox.Events;
using SIASUN.RCS.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Guids;
using Xunit;

namespace SIASUN.RCS.Domain.Tests.Outbox
{
    /// <summary>
    /// Outbox 事务可靠消息队列与 SAGA 补偿协调器单元测试
    /// </summary>
    public class OutboxQueueTests
    {
        /// <summary>
        /// 测试 Outbox 消息创建与生命周期流转（失败重试、标记成功、进入死信）
        /// </summary>
        [Fact]
        public void OutboxMessage_Lifecycle_Should_Work_Correctly()
        {
            var message = new OutboxMessage(
                Guid.NewGuid(),
                "TaskCompletedEvent",
                "{\"TaskId\":\"123\"}",
                "MES",
                "TRACE_001",
                maxRetries: 3);

            message.Status.ShouldBe(OutboxMessageStatus.Pending);
            message.RetryCount.ShouldBe(0);
            message.TraceId.ShouldBe("TRACE_001");
            message.Destination.ShouldBe("MES");

            // 第一次失败重试
            message.RecordRetryFailure("Network timeout 504", TimeSpan.FromSeconds(2));
            message.RetryCount.ShouldBe(1);
            message.Status.ShouldBe(OutboxMessageStatus.Publishing);
            message.LastError.ShouldNotBeNull();
            message.LastError.ShouldContain("504");
            message.NextRetryTime.ShouldNotBeNull();

            // 第二次失败重试
            message.RecordRetryFailure("Connection refused", TimeSpan.FromSeconds(4));
            message.RetryCount.ShouldBe(2);
            message.Status.ShouldBe(OutboxMessageStatus.Publishing);

            // 第三次失败，耗尽重试次数，转入死信队列
            message.RecordRetryFailure("Host unreachable", TimeSpan.FromSeconds(8));
            message.RetryCount.ShouldBe(3);
            message.Status.ShouldBe(OutboxMessageStatus.DeadLetter);

            // 重新创建一条成功消息
            var successMessage = new OutboxMessage(
                Guid.NewGuid(),
                "CarrierBoundEvent",
                "{\"CarrierCode\":\"FOUP_01\"}",
                "WMS",
                "TRACE_002");

            successMessage.MarkAsPublishing();
            successMessage.Status.ShouldBe(OutboxMessageStatus.Publishing);

            successMessage.MarkAsPublished();
            successMessage.Status.ShouldBe(OutboxMessageStatus.Published);
            successMessage.ProcessedTime.ShouldNotBeNull();
        }

        /// <summary>
        /// 测试通过 OutboxQueue 入队领域事件消息
        /// </summary>
        [Fact]
        public async Task OutboxQueue_Enqueue_Should_Insert_Message()
        {
            var repository = Substitute.For<IRepository<OutboxMessage, Guid>>();
            var guidGenerator = Substitute.For<IGuidGenerator>();
            guidGenerator.Create().Returns(Guid.NewGuid());
            OutboxMessage? captured = null;
            await repository.InsertAsync(Arg.Do<OutboxMessage>(m => captured = m), Arg.Any<bool>());

            var queue = new OutboxQueue(repository, guidGenerator, NullLogger<OutboxQueue>.Instance);

            var message = await queue.EnqueueAsync(
                "TaskDispatchLegReport",
                new { TaskId = "T01", Leg = "Fetch" },
                destination: "AMA",
                traceId: "TRACE_AMA_123");

            message.ShouldNotBeNull();
            message.Id.ShouldNotBe(Guid.Empty);
            captured.ShouldNotBeNull();
            captured.EventType.ShouldBe("TaskDispatchLegReport");
            captured.Destination.ShouldBe("AMA");
            captured.TraceId.ShouldBe("TRACE_AMA_123");
            captured.Payload.ShouldContain("Fetch");
        }

        /// <summary>
        /// 测试 SAGA 补偿协调器在不可恢复故障时发布本地事件并将补偿载荷写入 Outbox
        /// </summary>
        [Fact]
        public async Task SagaCompensationCoordinator_Should_Publish_LocalEvent_And_Enqueue_Outbox()
        {
            var queue = Substitute.For<IOutboxQueue>();
            var localEventBus = Substitute.For<ILocalEventBus>();
            var coordinator = new SagaCompensationCoordinator(queue, localEventBus, NullLogger<SagaCompensationCoordinator>.Instance);

            var task = new AgvTask(Guid.NewGuid(), "TASK_SAGA_01");
            task.Start(Guid.NewGuid(), "AGV_10");
            task.AdvanceStep(2, "Step2_Fetch");

            await coordinator.TriggerCompensationAsync(
                taskId: task.Id,
                taskCode: task.TaskCode,
                failedStepIndex: 2,
                failureReason: "TM execution aborted due to arm collision",
                compensationAction: "RetractGripper",
                destination: "MES",
                traceId: "TRACE_SAGA_999");

            // 验证 1：已向本地事件总线广播 SagaCompensationRequiredEvent
            await localEventBus.Received(1).PublishAsync(
                Arg.Is<SagaCompensationRequiredEvent>(e =>
                    e.TaskId == task.Id &&
                    e.TaskCode == "TASK_SAGA_01" &&
                    e.FailedStepIndex == 2 &&
                    e.FailureReason.Contains("arm collision") &&
                    e.CompensationAction == "RetractGripper" &&
                    e.TraceId == "TRACE_SAGA_999"));

            // 验证 2：已向 Outbox 队列入队持久化事务消息
            await queue.Received(1).EnqueueAsync(
                eventType: "SagaCompensationRequired",
                payload: Arg.Any<object>(),
                destination: "MES",
                traceId: "TRACE_SAGA_999",
                maxRetries: 5);
        }
    }
}
