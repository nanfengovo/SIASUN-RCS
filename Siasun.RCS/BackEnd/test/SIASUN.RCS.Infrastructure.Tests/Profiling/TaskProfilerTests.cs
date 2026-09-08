using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Infrastructure.Logging.Profiling;
using SIASUN.RCS.Profiling;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Tests.Profiling
{
    /// <summary>
    /// 任务步骤剖析器单元测试
    /// 验证 using 作用域自动秒表计时、实体组装以及无锁通道写入
    /// </summary>
    public class TaskProfilerTests
    {
        private readonly TaskProfilingChannel _channel;
        private readonly ILogger<TaskProfiler> _logger;
        private readonly TaskProfiler _profiler;

        public TaskProfilerTests()
        {
            _channel = new TaskProfilingChannel();
            _logger = Substitute.For<ILogger<TaskProfiler>>();
            _profiler = new TaskProfiler(_channel, _logger);
        }

        [Fact]
        public async Task BeginStep_WhenUsingScope_ShouldCalculateDurationAndWriteToChannel()
        {
            // Arrange & Act
            using (_profiler.BeginStep(
                taskCode: "TASK-TEST-001",
                subsystem: ProfilingSubsystem.PLC,
                operationName: "ReadErackSensor",
                agvId: "AGV-01",
                stepIndex: 1,
                activeLeg: "Fetch",
                summary: "读取传感器"))
            {
                await Task.Delay(20);
            }

            // Assert
            var hasData = _channel.Reader.TryRead(out var item);
            hasData.ShouldBeTrue();
            item.ShouldNotBeNull();
            item.TaskCode.ShouldBe("TASK-TEST-001");
            item.Subsystem.ShouldBe(ProfilingSubsystem.PLC);
            item.OperationName.ShouldBe("ReadErackSensor");
            item.AgvId.ShouldBe("AGV-01");
            item.StepIndex.ShouldBe(1);
            item.ActiveLeg.ShouldBe("Fetch");
            item.DurationMs.ShouldBeGreaterThanOrEqualTo(15);
            item.Status.ShouldBe("Success");
        }

        [Fact]
        public void RecordStep_DirectParameters_ShouldWriteToChannel()
        {
            // Arrange & Act
            _profiler.RecordStep(
                taskCode: "TASK-TEST-002",
                subsystem: ProfilingSubsystem.LocationLock,
                operationName: "TryLockAsync",
                durationMs: 45,
                status: "Success",
                summary: "加锁成功",
                agvId: "AGV-02");

            // Assert
            var hasData = _channel.Reader.TryRead(out var item);
            hasData.ShouldBeTrue();
            item.ShouldNotBeNull();
            item.TaskCode.ShouldBe("TASK-TEST-002");
            item.Subsystem.ShouldBe(ProfilingSubsystem.LocationLock);
            item.OperationName.ShouldBe("TryLockAsync");
            item.DurationMs.ShouldBe(45);
            item.Status.ShouldBe("Success");
            item.AgvId.ShouldBe("AGV-02");
        }

        [Fact]
        public void RecordStep_RecordObject_ShouldWriteToChannel()
        {
            // Arrange
            var record = new TaskStepProfilingRecord
            {
                TaskCode = "TASK-TEST-003",
                Subsystem = ProfilingSubsystem.TM,
                OperationName = "DispatchLeg",
                StartTime = DateTime.UtcNow.AddMilliseconds(-80),
                EndTime = DateTime.UtcNow,
                DurationMs = 80,
                Status = "Success",
                Summary = "下发 TM 程段成功",
                AgvId = "AGV-03",
                StepIndex = 2,
                ActiveLeg = "Put",
                BatchId = "BATCH-01",
                Details = """{"serialNo":"SN-001"}"""
            };

            // Act
            _profiler.RecordStep(record);

            // Assert
            var hasData = _channel.Reader.TryRead(out var item);
            hasData.ShouldBeTrue();
            item.ShouldNotBeNull();
            item.TaskCode.ShouldBe("TASK-TEST-003");
            item.Subsystem.ShouldBe(ProfilingSubsystem.TM);
            item.OperationName.ShouldBe("DispatchLeg");
            item.DurationMs.ShouldBe(80);
            item.BatchId.ShouldBe("BATCH-01");
            item.Details.ShouldBe("""{"serialNo":"SN-001"}""");
        }
    }
}
