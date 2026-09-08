using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Logs.OperatorLog;
using SIASUN.RCS.Logs.OperatorLogs;
using SIASUN.RCS.Profiling;
using SIASUN.RCS.Tasks;
using SIASUN.RCS.Tasks.Dtos;
using SIASUN.RCS.Tasks.Profiling;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Xunit;

namespace SIASUN.RCS.Application.Tests.Tasks
{
    /// <summary>
    /// 任务步骤剖析与流转看板应用服务单元测试
    /// 验证任务画像生成、TM阶段组装、子系统耗时拆解与 Dashboard 统计指标计算
    /// </summary>
    public class TaskProfilingAppServiceTests
    {
        private readonly IRepository<TaskStepProfiling, Guid> _profilingRepo;
        private readonly IRepository<AgvTask, Guid> _taskRepo;
        private readonly IRepository<OperationLog, Guid> _opRepo;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly TaskProfilingAppService _appService;

        public TaskProfilingAppServiceTests()
        {
            _profilingRepo = Substitute.For<IRepository<TaskStepProfiling, Guid>>();
            _taskRepo = Substitute.For<IRepository<AgvTask, Guid>>();
            _opRepo = Substitute.For<IRepository<OperationLog, Guid>>();
            _asyncExecuter = Substitute.For<IAsyncQueryableExecuter>();

            _asyncExecuter.ToListAsync(Arg.Any<IQueryable<AgvTask>>(), Arg.Any<System.Threading.CancellationToken>())
                .Returns(ci => Task.FromResult(ci.ArgAt<IQueryable<AgvTask>>(0).ToList()));
            _asyncExecuter.ToListAsync(Arg.Any<IQueryable<TaskStepProfiling>>(), Arg.Any<System.Threading.CancellationToken>())
                .Returns(ci => Task.FromResult(ci.ArgAt<IQueryable<TaskStepProfiling>>(0).ToList()));
            _asyncExecuter.FirstOrDefaultAsync(Arg.Any<IQueryable<AgvTask>>(), Arg.Any<System.Threading.CancellationToken>())
                .Returns(ci => Task.FromResult(ci.ArgAt<IQueryable<AgvTask>>(0).FirstOrDefault()));
            _asyncExecuter.CountAsync(Arg.Any<IQueryable<OperationLog>>(), Arg.Any<System.Threading.CancellationToken>())
                .Returns(ci => Task.FromResult(ci.ArgAt<IQueryable<OperationLog>>(0).Count()));
            _asyncExecuter.CountAsync(Arg.Any<IQueryable<TaskStepProfiling>>(), Arg.Any<System.Threading.CancellationToken>())
                .Returns(ci => Task.FromResult(ci.ArgAt<IQueryable<TaskStepProfiling>>(0).Count()));

            _appService = new TaskProfilingAppService(
                _profilingRepo,
                _taskRepo,
                _opRepo,
                _asyncExecuter);
        }

        [Fact]
        public async Task GetTaskTimelineProfilingAsync_Should_AssembleStages_Interactions_And_Breakdown()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var task = new AgvTask(
                id: Guid.NewGuid(),
                taskCode: "TASK-001",
                fromStation: "ST-01",
                toStation: "ST-02",
                carrierCode: "FOUP-01",
                batchId: "BATCH-888",
                traceId: "trace-123");
            task.Start(Guid.NewGuid(), "AGV-01");
            task.AdvanceStep(1, "Fetch", "WaitingTM");

            var steps = new List<TaskStepProfiling>
            {
                new(Guid.NewGuid(), "TASK-001", ProfilingSubsystem.AMA, "ReceiveTask", now.AddSeconds(-100), now.AddSeconds(-99), 100, "Success", "接收任务", "BATCH-888", "AGV-01"),
                new(Guid.NewGuid(), "TASK-001", ProfilingSubsystem.PLC, "ReadSensor", now.AddSeconds(-90), now.AddSeconds(-89), 30, "Success", "读取光电", "BATCH-888", "AGV-01"),
                new(Guid.NewGuid(), "TASK-001", ProfilingSubsystem.LocationLock, "AcquireLock", now.AddSeconds(-80), now.AddSeconds(-79), 20, "Success", "锁定库位", "BATCH-888", "AGV-01"),
                new(Guid.NewGuid(), "TASK-001", ProfilingSubsystem.TM, "DispatchLeg", now.AddSeconds(-70), now.AddSeconds(-69), 80, "Success", "下发程段", "BATCH-888", "AGV-01", activeLeg: "Fetch")
            };

            _taskRepo.GetQueryableAsync().Returns(Task.FromResult(new List<AgvTask> { task }.AsQueryable()));
            _profilingRepo.GetQueryableAsync().Returns(Task.FromResult(steps.AsQueryable()));

            // Act
            var result = await _appService.GetTaskTimelineProfilingAsync("TASK-001");

            // Assert
            result.ShouldNotBeNull();
            result.TaskCode.ShouldBe("TASK-001");
            result.FromStation.ShouldBe("ST-01");
            result.ToStation.ShouldBe("ST-02");
            result.CarrierCode.ShouldBe("FOUP-01");
            result.AssignedVehicleCode.ShouldBe("AGV-01");

            // 验证 TM 流转阶段
            result.TmStages.Count.ShouldBe(5);
            result.TmStages[0].Stage.ShouldBe("创建");
            result.TmStages[0].State.ShouldBe("Finished");
            result.TmStages[1].Stage.ShouldBe("已派发");
            result.TmStages[1].State.ShouldBe("Finished");
            result.TmStages[2].Stage.ShouldBe("取货");
            result.TmStages[2].State.ShouldBe("Running");

            // 验证子系统交互流水
            result.Interactions.Count.ShouldBe(4);
            result.Interactions[0].Subsystem.ShouldBe(ProfilingSubsystem.AMA);
            result.Interactions[0].ElapsedMs.ShouldBe(100);
            result.Interactions[1].Subsystem.ShouldBe(ProfilingSubsystem.PLC);
            result.Interactions[1].ElapsedMs.ShouldBe(30);
            result.Interactions[2].Subsystem.ShouldBe(ProfilingSubsystem.LocationLock);
            result.Interactions[2].ElapsedMs.ShouldBe(20);
            result.Interactions[3].Subsystem.ShouldBe(ProfilingSubsystem.TM);
            result.Interactions[3].ElapsedMs.ShouldBe(80);

            // 验证耗时拆解
            result.MetricsBreakdown.AmaInteractionMs.ShouldBe(100);
            result.MetricsBreakdown.PlcInteractionMs.ShouldBe(30);
            result.MetricsBreakdown.LocationLockMs.ShouldBe(20);
            result.MetricsBreakdown.TmDispatchMs.ShouldBe(80);
        }

        [Fact]
        public async Task GetMetricsSummaryAsync_Should_CalculateAggregates_For_Dashboard()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var startTime = now.AddHours(-2);
            var endTime = now.AddHours(2);

            var task1 = new AgvTask(Guid.NewGuid(), "T-101");
            task1.Start(Guid.NewGuid(), "AGV-01", startTime: now.AddMinutes(-30));
            task1.Complete(now.AddMinutes(-28)); // 120s = 120000ms

            var task2 = new AgvTask(Guid.NewGuid(), "T-102");
            task2.Start(Guid.NewGuid(), "AGV-02", startTime: now.AddMinutes(-10));
            task2.Complete(now.AddMinutes(-9)); // 60s = 60000ms

            var tasks = new List<AgvTask> { task1, task2 };

            var steps = new List<TaskStepProfiling>
            {
                new(Guid.NewGuid(), "T-101", ProfilingSubsystem.Arm, "PickCarrier", now.AddMinutes(-30), now.AddMinutes(-30).AddSeconds(15), 15000, "Success", "机械臂取料"),
                new(Guid.NewGuid(), "T-102", ProfilingSubsystem.Arm, "PlaceCarrier", now.AddMinutes(-10), now.AddMinutes(-10).AddSeconds(20), 20000, "Success", "机械臂放料"),
                new(Guid.NewGuid(), "T-101", ProfilingSubsystem.TM, "MoveToStation", now.AddMinutes(-20), now.AddMinutes(-20).AddSeconds(30), 30000, "Success", "小车走行"),
                new(Guid.NewGuid(), "T-101", ProfilingSubsystem.Vision, "AlignVision", now.AddMinutes(-30), now.AddMinutes(-30).AddSeconds(3), 3000, "Success", "视觉拍照"),
                new(Guid.NewGuid(), "T-102", ProfilingSubsystem.TrafficControl, "WaitTrafficLock", now.AddMinutes(-10), now.AddMinutes(-10).AddSeconds(2), 2000, "Success", "交管等待")
            };

            var opLogs = new List<OperationLog>
            {
                new(Guid.NewGuid(), OperatorType.User, Guid.NewGuid(), "Admin", "127.0.0.1", "c1", "Lock", "ForceUnlock", "Lock", "ST-1", OperationLogStatus.Success, "人工解锁", null, now.AddMinutes(-5)),
                new(Guid.NewGuid(), OperatorType.User, Guid.NewGuid(), "Admin", "127.0.0.1", "c2", "Task", "CancelTask", "Task", "T-101", OperationLogStatus.Success, "人工取消", null, now.AddMinutes(-3)),
                new(Guid.NewGuid(), OperatorType.System, null, "AutoWorker", "127.0.0.1", "c3", "Sweep", "AutoReclaim", "Lock", "ST-2", OperationLogStatus.Success, "系统自愈", null, now.AddMinutes(-1))
            };

            _taskRepo.GetQueryableAsync().Returns(Task.FromResult(tasks.AsQueryable()));
            _profilingRepo.GetQueryableAsync().Returns(Task.FromResult(steps.AsQueryable()));
            _opRepo.GetQueryableAsync().Returns(Task.FromResult(opLogs.AsQueryable()));

            // Act
            var summary = await _appService.GetMetricsSummaryAsync(new GetMetricsSummaryInput
            {
                StartTime = startTime,
                EndTime = endTime
            });

            // Assert
            summary.ShouldNotBeNull();
            summary.TotalTasks.ShouldBe(2);
            summary.SucceededTasks.ShouldBe(2);
            summary.FailedTasks.ShouldBe(0);

            // 任务总耗时 (60000ms & 120000ms)
            summary.TotalTaskDuration.SampleCount.ShouldBe(2);
            summary.TotalTaskDuration.AverageMs.ShouldBe(90000);
            summary.TotalTaskDuration.MinMs.ShouldBe(60000);
            summary.TotalTaskDuration.MaxMs.ShouldBe(120000);

            // 机械臂动作耗时 (15000ms & 20000ms)
            summary.ArmActionDuration.SampleCount.ShouldBe(2);
            summary.ArmActionDuration.AverageMs.ShouldBe(17500);
            summary.ArmActionDuration.MinMs.ShouldBe(15000);
            summary.ArmActionDuration.MaxMs.ShouldBe(20000);

            // 视觉与交管
            summary.VisualAlignDuration.SampleCount.ShouldBe(1);
            summary.VisualAlignDuration.AverageMs.ShouldBe(3000);
            summary.TrafficWaitDuration.SampleCount.ShouldBe(1);
            summary.TrafficWaitDuration.AverageMs.ShouldBe(2000);

            // 人工干预次数（支持 MTBA 指标）
            summary.ManualInterventionCount.ShouldBe(2);

            // 子系统平均耗时字典
            summary.SubsystemAvgLatencies.ContainsKey(ProfilingSubsystem.Arm).ShouldBeTrue();
            summary.SubsystemAvgLatencies[ProfilingSubsystem.Arm].ShouldBe(17500);
        }

        [Fact]
        public async Task GetStepListAsync_Should_FilterAndPaginate()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var steps = new List<TaskStepProfiling>
            {
                new(Guid.NewGuid(), "T-001", ProfilingSubsystem.PLC, "Read1", now.AddSeconds(-10), now, 100, "Success", "s1"),
                new(Guid.NewGuid(), "T-001", ProfilingSubsystem.PLC, "Read2", now.AddSeconds(-5), now, 200, "Success", "s2"),
                new(Guid.NewGuid(), "T-002", ProfilingSubsystem.TM, "Move", now.AddSeconds(-2), now, 500, "Success", "s3")
            };

            _profilingRepo.GetQueryableAsync().Returns(Task.FromResult(steps.AsQueryable()));

            // Act
            var result = await _appService.GetStepListAsync(new GetTaskStepListInput
            {
                TaskCode = "T-001",
                SkipCount = 0,
                MaxResultCount = 10
            });

            // Assert
            result.ShouldNotBeNull();
            result.TotalCount.ShouldBe(2);
            result.Items.Count.ShouldBe(2);
            result.Items.All(i => i.TaskCode == "T-001").ShouldBeTrue();
        }
    }
}
