using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Infrastructure.Logging;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Tests
{
    public class EntityAuditLogConsumerTests
    {
        private static (EntityAuditLogChannel Channel, string SpillDir) CreateIsolatedChannel()
        {
            var dir = Path.Combine(System.IO.Path.GetTempPath(), "rcs_entity_test_" + Guid.NewGuid().ToString("N"));
            return (new EntityAuditLogChannel(spillDir: dir), dir);
        }

        [Fact]
        public async Task Should_Consume_Messages_And_Save()
        {
            var (channel, spillDir) = CreateIsolatedChannel();
            try
            {
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
            finally
            {
                try { if (System.IO.Directory.Exists(spillDir)) System.IO.Directory.Delete(spillDir, true); } catch { }
            }
        }

        [Fact]
        public async Task Should_Throttle_Non_Critical_Entities_Under_CriticalBurst_While_Preserving_Core_AgvTasks()
        {
            var (channel, spillDir) = CreateIsolatedChannel();
            try
            {
                var mockStore = Substitute.For<IEntityAuditLogStore>();
                IReadOnlyList<EntityAuditLogEntry>? capturedBatch = null;
                _ = mockStore.SaveBatchAsync(Arg.Do<IReadOnlyList<EntityAuditLogEntry>>(b => capturedBatch = new List<EntityAuditLogEntry>(b)), Arg.Any<CancellationToken>());

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

                var cts = new CancellationTokenSource();
                var task = consumer.StartAsync(cts.Token);

                for (int i = 0; i < 20 && capturedBatch == null; i++)
                {
                    await Task.Delay(100);
                }

                cts.Cancel();
                try { await task; } catch { }

                // 断言：持久化只包含了 AgvTask，SensorData 在洪峰下被限流丢弃保盘
                capturedBatch.ShouldNotBeNull();
                capturedBatch.Count.ShouldBe(1);
                capturedBatch[0].EntityName.ShouldBe("AgvTask");
                capturedBatch[0].EntityId.ShouldBe("TASK-001");
            }
            finally
            {
                try { if (System.IO.Directory.Exists(spillDir)) System.IO.Directory.Delete(spillDir, true); } catch { }
            }
        }

        [Fact]
        public async Task Should_Spill_Privileged_Entities_To_Buffer_When_Store_Fails()
        {
            var (channel, spillDir) = CreateIsolatedChannel();
            try
            {
                var mockStore = Substitute.For<IEntityAuditLogStore>();
                mockStore.SaveBatchAsync(Arg.Any<IReadOnlyList<EntityAuditLogEntry>>(), Arg.Any<CancellationToken>())
                    .ThrowsAsync(new InvalidOperationException("DB Deadlock"));

                var consumer = new EntityAuditLogConsumer(
                    channel,
                    mockStore,
                    NullLogger<EntityAuditLogConsumer>.Instance
                );

                channel.TryWrite(new EntityAuditLogMessage
                {
                    TraceId = "TRACE-TASK-SPILL",
                    EntityName = "AgvTask",
                    EntityId = "TASK-002",
                    Action = "Modified",
                    CreationTime = DateTime.UtcNow
                });
                channel.TryWrite(new EntityAuditLogMessage
                {
                    TraceId = "TRACE-TEMP-SPILL",
                    EntityName = "TempWorker",
                    EntityId = "TMP-001",
                    Action = "Modified",
                    CreationTime = DateTime.UtcNow
                });

                var cts = new CancellationTokenSource();
                var task = consumer.StartAsync(cts.Token);

                for (int i = 0; i < 20 && channel.PendingSpillCount == 0; i++)
                {
                    await Task.Delay(100);
                }

                cts.Cancel();
                try { await task; } catch { }

                var items = new List<EntityAuditLogMessage>();
                while (channel.SpillBuffer.TryDequeue(out var it))
                {
                    items.Add(it);
                }

                // Assert: Core entity AgvTask was spilled back to SpillBuffer, non-core TempWorker was not spilled
                items.Count.ShouldBe(1, $"Found {items.Count} items: " + string.Join(", ", System.Linq.Enumerable.Select(items, x => $"{x.EntityName}:{x.EntityId}:{x.TraceId}")));
                items[0].EntityName.ShouldBe("AgvTask");
                items[0].EntityId.ShouldBe("TASK-002");
            }
            finally
            {
                try { if (System.IO.Directory.Exists(spillDir)) System.IO.Directory.Delete(spillDir, true); } catch { }
            }
        }

        [Fact]
        public async Task Should_Spill_Privileged_Entities_When_Exception_Occurs_During_Batch_Assembly()
        {
            var (channel, spillDir) = CreateIsolatedChannel();
            try
            {
                var mockStore = Substitute.For<IEntityAuditLogStore>();
                var mockBroker = Substitute.For<SIASUN.RCS.Infrastructure.Logging.Diagnostics.SignalR.IDiagnosticLiveStreamBroker>();
                mockBroker.IsEnabled.Returns(true);
                mockBroker.When(b => b.Publish(Arg.Any<SIASUN.RCS.Infrastructure.Logging.Diagnostics.SignalR.LiveEventDto>()))
                    .Do(_ => throw new InvalidOperationException("Broker pipeline shattered"));

                var consumer = new EntityAuditLogConsumer(
                    channel,
                    mockStore,
                    NullLogger<EntityAuditLogConsumer>.Instance,
                    liveStreamBroker: mockBroker
                );

                // 写入一条核心特权实体变更
                channel.TryWrite(new EntityAuditLogMessage
                {
                    TraceId = "TRACE-ASSEMBLY-FAIL",
                    EntityName = "AgvTask",
                    EntityId = "TASK-003",
                    Action = "Created",
                    CreationTime = DateTime.UtcNow
                });

                var cts = new CancellationTokenSource();
                var task = consumer.StartAsync(cts.Token);

                for (int i = 0; i < 20 && channel.PendingSpillCount == 0; i++)
                {
                    await Task.Delay(100);
                }

                cts.Cancel();
                try { await task; } catch { }

                // 验证：即使在攒批阶段因外部依赖崩溃抛异常，已入批的特权铁证依然被 outer catch 回灌 SpillBuffer，杜绝静默清空丢证
                channel.PendingSpillCount.ShouldBe(1);
                channel.SpillBuffer.TryDequeue(out var rescued).ShouldBeTrue();
                rescued.ShouldNotBeNull();
                rescued.EntityName.ShouldBe("AgvTask");
                rescued.EntityId.ShouldBe("TASK-003");
            }
            finally
            {
                try { if (System.IO.Directory.Exists(spillDir)) System.IO.Directory.Delete(spillDir, true); } catch { }
            }
        }
    }
}
