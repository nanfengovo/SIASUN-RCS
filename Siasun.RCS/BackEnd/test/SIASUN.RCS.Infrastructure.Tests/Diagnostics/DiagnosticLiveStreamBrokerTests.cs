using System;
using System.Linq;
using Microsoft.Extensions.Options;
using Shouldly;
using SIASUN.RCS.Infrastructure.Logging.Diagnostics.SignalR;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Tests.Diagnostics
{
    public class DiagnosticLiveStreamBrokerTests
    {
        [Fact]
        public void Publish_WhenDisabled_ShouldNotRecordOrQueue()
        {
            var options = Options.Create(new SignalRDiagnosticsOptions
            {
                IsEnabled = false,
                RingBufferCapacity = 50
            });

            var broker = new DiagnosticLiveStreamBroker(options);
            broker.IsEnabled.ShouldBeFalse();

            var evt = new LiveEventDto
            {
                Track = "API",
                Level = "Information",
                Title = "Test API",
                TargetId = "T-1001",
                VehicleId = "AGV-01"
            };

            broker.Publish(evt);

            broker.GetHistory("all").ShouldBeEmpty();
            broker.GetHistory("task:T-1001").ShouldBeEmpty();
            broker.GetHistory("vehicle:AGV-01").ShouldBeEmpty();

            var batches = broker.DequeuePendingBatches();
            batches.ShouldBeEmpty();
        }

        [Fact]
        public void Publish_WhenEnabled_ShouldRouteToCorrectTopics()
        {
            var options = Options.Create(new SignalRDiagnosticsOptions
            {
                IsEnabled = true,
                RingBufferCapacity = 50
            });

            var broker = new DiagnosticLiveStreamBroker(options);
            broker.IsEnabled.ShouldBeTrue();

            // 1. Info event with task and vehicle
            var infoEvt = new LiveEventDto
            {
                Track = "API",
                Level = "Information",
                Title = "MES Create Task",
                TargetId = "T-2001",
                VehicleId = "AGV-02"
            };
            broker.Publish(infoEvt);

            // 2. Error event with task only
            var errorEvt = new LiveEventDto
            {
                Track = "Exception",
                Level = "Error",
                Title = "Station Collision",
                TargetId = "T-2001"
            };
            broker.Publish(errorEvt);

            // 3. Warning event with vehicle only
            var warnEvt = new LiveEventDto
            {
                Track = "Operator",
                Level = "Warning",
                Title = "Battery Low",
                VehicleId = "AGV-02"
            };
            broker.Publish(warnEvt);

            // Check "all" topic -> all 3 events
            var allHistory = broker.GetHistory("all");
            allHistory.Count.ShouldBe(3);

            // Check "errors" topic -> 2 events (Error & Warning)
            var errorHistory = broker.GetHistory("errors");
            errorHistory.Count.ShouldBe(2);
            errorHistory.Any(e => e.Title == "Station Collision").ShouldBeTrue();
            errorHistory.Any(e => e.Title == "Battery Low").ShouldBeTrue();

            // Check "task:T-2001" topic -> 2 events (infoEvt, errorEvt)
            var taskHistory = broker.GetHistory("task:T-2001");
            taskHistory.Count.ShouldBe(2);

            // Check "vehicle:AGV-02" topic -> 2 events (infoEvt, warnEvt)
            var vehicleHistory = broker.GetHistory("vehicle:AGV-02");
            vehicleHistory.Count.ShouldBe(2);

            // Dequeue pending batches
            var batches = broker.DequeuePendingBatches();
            batches.ContainsKey("all").ShouldBeTrue();
            batches.ContainsKey("errors").ShouldBeTrue();
            batches.ContainsKey("task:T-2001").ShouldBeTrue();
            batches.ContainsKey("vehicle:AGV-02").ShouldBeTrue();

            // Next dequeue should be empty
            var emptyBatches = broker.DequeuePendingBatches();
            emptyBatches.ShouldBeEmpty();
        }

        [Fact]
        public void RingBuffer_ShouldEnforceCapacityLimit()
        {
            var options = Options.Create(new SignalRDiagnosticsOptions
            {
                IsEnabled = true,
                RingBufferCapacity = 3
            });

            var broker = new DiagnosticLiveStreamBroker(options);

            for (int i = 1; i <= 5; i++)
            {
                broker.Publish(new LiveEventDto
                {
                    Track = "API",
                    Level = "Information",
                    Title = $"Event {i}"
                });
            }

            var history = broker.GetHistory("all");
            history.Count.ShouldBe(3);
            history[0].Title.ShouldBe("Event 3");
            history[1].Title.ShouldBe("Event 4");
            history[2].Title.ShouldBe("Event 5");
        }

        [Fact]
        public void Publish_WithMinLogLevel_ShouldFilterOutLowerSeverity()
        {
            var options = Options.Create(new SignalRDiagnosticsOptions
            {
                IsEnabled = true,
                MinLogLevel = "Warning"
            });

            var broker = new DiagnosticLiveStreamBroker(options);

            // Information event -> filtered out
            broker.Publish(new LiveEventDto
            {
                Track = "API",
                Level = "Information",
                Title = "Ignored Info Event"
            });

            // Warning event -> accepted
            broker.Publish(new LiveEventDto
            {
                Track = "API",
                Level = "Warning",
                Title = "Accepted Warn Event"
            });

            // Error event -> accepted
            broker.Publish(new LiveEventDto
            {
                Track = "API",
                Level = "Error",
                Title = "Accepted Error Event"
            });

            var history = broker.GetHistory("all");
            history.Count.ShouldBe(2);
            history.Any(e => e.Title == "Ignored Info Event").ShouldBeFalse();
            history.Any(e => e.Title == "Accepted Warn Event").ShouldBeTrue();
            history.Any(e => e.Title == "Accepted Error Event").ShouldBeTrue();
        }

        [Fact]
        public void Publish_ExceedingMaxActiveTopics_ShouldEvictLeastRecentlyUsedTopics()
        {
            var options = Options.Create(new SignalRDiagnosticsOptions
            {
                IsEnabled = true,
                MaxActiveTopics = 3,
                RingBufferCapacity = 10
            });

            var broker = new DiagnosticLiveStreamBroker(options);

            // Publish events with 20 different task IDs
            for (int i = 1; i <= 20; i++)
            {
                broker.Publish(new LiveEventDto
                {
                    Track = "API",
                    Level = "Information",
                    Title = $"Task Event {i}",
                    TargetId = $"TASK-{i:D3}"
                });
            }

            // "all" topic is permanent and must never be evicted
            broker.GetHistory("all").ShouldNotBeEmpty();

            // The earliest dynamic topics like task:TASK-001 should have been evicted
            broker.GetHistory("task:TASK-001").ShouldBeEmpty();
            // The most recent dynamic topic should exist
            broker.GetHistory("task:TASK-020").ShouldNotBeEmpty();
        }

        [Fact]
        public void Publish_Under_CriticalBurst_Should_Throttle_Telemetry_While_Preserving_Errors_And_Operations()
        {
            var options = Options.Create(new SignalRDiagnosticsOptions
            {
                IsEnabled = true,
                RingBufferCapacity = 500
            });

            // 构造真实的 AdaptiveTrafficGovernor，设置极低的阈值以便在测试中快速触发 CriticalBurst
            var governor = new SIASUN.RCS.Diagnostics.AdaptiveTrafficGovernor(elevatedThresholdEps: 5, criticalThresholdEps: 10);

            var broker = new DiagnosticLiveStreamBroker(options, governor);

            // 1. 发送一批高频非关键遥测事件，迅速推高 EPS 触发 CriticalBurst 限流降采样
            for (int i = 0; i < 50; i++)
            {
                broker.Publish(new LiveEventDto
                {
                    Track = "Telemetry",
                    Level = "Information",
                    Title = $"Telemetry Sensor Ping {i}"
                });
            }

            // 2. 穿插发送关键铁证：Operation 操作事件与 Error/Warning 异常事件
            for (int i = 0; i < 10; i++)
            {
                broker.Publish(new LiveEventDto
                {
                    Track = "Operator",
                    Level = "Information",
                    Title = $"Operator Dispatch Intervention {i}",
                    TargetId = $"TASK-{i}"
                });

                broker.Publish(new LiveEventDto
                {
                    Track = "Exception",
                    Level = "Error",
                    Title = $"Hardware Collision Error {i}"
                });
            }

            var allEvents = broker.GetHistory("all", 500);
            var errorEvents = broker.GetHistory("errors", 500);

            // 核心断言 1：所有 10 条 Error 异常必须 100% 准入放行
            errorEvents.Count.ShouldBe(10);

            // 核心断言 2：所有 10 条 Operator 调度人工干预必须 100% 准入放行
            allEvents.Count(e => e.Track == "Operator").ShouldBe(10);

            // 核心断言 3：Telemetry 事件在洪峰状态下被自适应平滑降采样丢弃（准入数显著少于 50）
            var admittedTelemetryCount = allEvents.Count(e => e.Track == "Telemetry");
            admittedTelemetryCount.ShouldBeLessThan(50);

            var metrics = governor.GetMetrics();
            metrics.TotalDroppedCount.ShouldBeGreaterThan(0);
        }
    }
}
