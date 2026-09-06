using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Infrastructure.Logging;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Tests
{
    public class EntityAuditLogConsumerTests
    {
        [Fact]
        public async Task Should_Consume_Messages_And_Save()
        {
            var channel = new EntityAuditLogChannel();
            var mockStore = Substitute.For<IEntityAuditLogStore>();

            var consumer = new EntityAuditLogConsumer(
                channel,
                mockStore,
                NullLogger<EntityAuditLogConsumer>.Instance
            );

            var cts = new CancellationTokenSource();

            var task = consumer.StartAsync(cts.Token);

            var msg = new EntityAuditLogMessage
            {
                TraceId = "TEST-TRACE-1",
                EntityName = "TestEntity",
                EntityId = "T01",
                Action = "Modified",
                CreationTime = DateTime.UtcNow,
                ChangedProperties = new List<string> { "Name" }
            };

            channel.TryWrite(msg);

            await Task.Delay(200);
            
            cts.Cancel();
            try { await task; } catch { }

            // Assert
            await mockStore.Received(1).SaveBatchAsync(Arg.Any<IReadOnlyList<EntityAuditLogEntry>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Throttle_Non_Critical_Entities_Under_CriticalBurst_While_Preserving_Core_AgvTasks()
        {
            var channel = new EntityAuditLogChannel();
            var mockStore = Substitute.For<IEntityAuditLogStore>();
            IReadOnlyList<EntityAuditLogEntry>? capturedBatch = null;
            await mockStore.SaveBatchAsync(Arg.Do<IReadOnlyList<EntityAuditLogEntry>>(b => capturedBatch = new List<EntityAuditLogEntry>(b)), Arg.Any<CancellationToken>());

            var governor = Substitute.For<SIASUN.RCS.Diagnostics.IAdaptiveTrafficGovernor>();
            // 模拟限流策略：非关键实体 SensorData 丢弃，而核心调度实体 AgvTask 准入
            governor.ShouldAdmit("SensorData", "Information")
                .Returns(SIASUN.RCS.Diagnostics.TrafficSamplingDecision.Drop(SIASUN.RCS.Diagnostics.TrafficGovernorLevel.CriticalBurst, 0.1, "Storm"));
            governor.ShouldAdmit("AgvTask", "Information")
                .Returns(SIASUN.RCS.Diagnostics.TrafficSamplingDecision.Admit(SIASUN.RCS.Diagnostics.TrafficGovernorLevel.CriticalBurst));

            var consumer = new EntityAuditLogConsumer(
                channel,
                mockStore,
                NullLogger<EntityAuditLogConsumer>.Instance,
                liveStreamBroker: null,
                trafficGovernor: governor
            );

            var cts = new CancellationTokenSource();
            var task = consumer.StartAsync(cts.Token);

            // 写入一条非关键遥测实体变更
            channel.TryWrite(new EntityAuditLogMessage
            {
                TraceId = "TRACE-SENSOR",
                EntityName = "SensorData",
                EntityId = "S-001",
                Action = "Modified",
                CreationTime = DateTime.UtcNow
            });

            // 写入一条核心调度任务实体变更
            channel.TryWrite(new EntityAuditLogMessage
            {
                TraceId = "TRACE-TASK",
                EntityName = "AgvTask",
                EntityId = "TASK-001",
                Action = "Modified",
                CreationTime = DateTime.UtcNow
            });

            await Task.Delay(200);

            cts.Cancel();
            try { await task; } catch { }

            // 断言：持久化只包含了 AgvTask，SensorData 在洪峰下被限流丢弃保盘
            capturedBatch.ShouldNotBeNull();
            capturedBatch.Count.ShouldBe(1);
            capturedBatch[0].EntityName.ShouldBe("AgvTask");
            capturedBatch[0].EntityId.ShouldBe("TASK-001");
        }
    }
}
