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
    }
}
