using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Dispatch;
using SIASUN.RCS.Dispatch.Dtos;
using SIASUN.RCS.Interfaces.OperationLogs;
using SIASUN.RCS.Logs.OperatorLog;
using SIASUN.RCS.Logs.OperatorLogs;
using SIASUN.RCS.Tasks;
using SIASUN.RCS.Vehicles;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace SIASUN.RCS.Application.Tests.Dispatch
{
    /// <summary>
    /// 调度员人工干预应用服务单元测试
    /// 验证任务取消、强制完结、人工指派车辆、车辆复位等关键干预操作均 100% 记录责任审计
    /// 严格验证实体不存在时记录失败审计（BeforeState=null, AfterState=null）并抛出 EntityNotFoundException，绝不捏造状态
    /// </summary>
    public class DispatchInterventionAppServiceTests
    {
        private readonly IOperationLogRecorder _opRecorder;
        private readonly IRepository<AgvTask, Guid> _taskRepo;
        private readonly IRepository<AgvVehicle, Guid> _vehicleRepo;
        private readonly DispatchInterventionAppService _appService;

        public DispatchInterventionAppServiceTests()
        {
            _opRecorder = Substitute.For<IOperationLogRecorder>();
            _taskRepo = Substitute.For<IRepository<AgvTask, Guid>>();
            _vehicleRepo = Substitute.For<IRepository<AgvVehicle, Guid>>();

            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            services.AddSingleton(_opRecorder);
            services.AddSingleton(_taskRepo);
            services.AddSingleton(_vehicleRepo);
            services.AddLogging();
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(RCSApplicationModule).Assembly);
                cfg.AddOpenBehavior(typeof(SIASUN.RCS.Commands.CommandAuditPipelineBehavior<,>));
            });

            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<MediatR.IMediator>();
            _appService = new DispatchInterventionAppService(mediator);
        }

        [Fact]
        public async Task CancelTaskAsync_WithoutReason_Should_ThrowException()
        {
            var input = new CancelTaskInput
            {
                TaskId = "TASK-1001",
                Reason = "" // 缺失原因
            };

            await Should.ThrowAsync<UserFriendlyException>(async () =>
            {
                await _appService.CancelTaskAsync(input);
            });
        }

        [Fact]
        public async Task CancelTaskAsync_WhenTaskExists_Should_MutateEntity_And_RecordOperationLog()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var task = new AgvTask(taskId, "TASK-1001", "ST-01", "ST-02");
            task.Start(Guid.NewGuid(), "AGV-01", "TRACE-01");

            _taskRepo.FindAsync(Arg.Any<Expression<Func<AgvTask, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<AgvTask?>(task));

            var input = new CancelTaskInput
            {
                TaskId = "TASK-1001",
                AgvId = "AGV-01",
                Reason = "产线急停，调度员人工取消此任务"
            };

            // Act
            var result = await _appService.CancelTaskAsync(input);

            // Assert
            result.Success.ShouldBeTrue();
            result.TargetType.ShouldBe("Task");
            result.TargetId.ShouldBe("TASK-1001");
            result.CurrentState.ShouldBe("Canceled");
            task.Status.ShouldBe(AgvTaskStatus.Canceled);

            await _taskRepo.Received(1).UpdateAsync(task, autoSave: true);

            _opRecorder.Received(1).Record(Arg.Is<OperationLogContext>(ctx =>
                ctx.Module == "Dispatch" &&
                ctx.Action == "CancelTask" &&
                ctx.TargetId == "TASK-1001" &&
                ctx.TaskId == "TASK-1001" &&
                ctx.AgvId == "AGV-01" &&
                ctx.BeforeState == "Running" &&
                ctx.AfterState == "Canceled" &&
                ctx.Reason == input.Reason
            ), OperationLogStatus.Success, null);
        }

        [Fact]
        public async Task CancelTaskAsync_WhenTaskNotFoundInRepo_Should_RecordFailure_And_ThrowEntityNotFound()
        {
            // Arrange
            _taskRepo.FindAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<AgvTask?>(null));
            _taskRepo.FirstOrDefaultAsync(Arg.Any<Expression<Func<AgvTask, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<AgvTask?>(null));

            var input = new CancelTaskInput
            {
                TaskId = "NON-EXISTENT-TASK",
                Reason = "任务超时人工取消"
            };

            // Act & Assert: 必须抛出 EntityNotFoundException
            var ex = await Should.ThrowAsync<EntityNotFoundException>(async () =>
            {
                await _appService.CancelTaskAsync(input);
            });
            ex.EntityType.ShouldBe(typeof(AgvTask));

            // 严格对齐 L3：实体不存在时，BeforeState 与 AfterState 必须为 null，绝不捏造 "NonExistent" 伪状态
            _opRecorder.Received(1).Record(Arg.Is<OperationLogContext>(ctx =>
                ctx.Module == "Dispatch" &&
                ctx.Action == "CancelTask" &&
                ctx.TargetId == "NON-EXISTENT-TASK" &&
                ctx.BeforeState == null &&
                ctx.AfterState == null
            ), OperationLogStatus.Failed, Arg.Any<string>());
        }

        [Fact]
        public async Task ForceEndTaskAsync_WhenTaskNotFoundInRepo_Should_RecordFailure_And_ThrowEntityNotFound()
        {
            // Arrange
            _taskRepo.FindAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<AgvTask?>(null));
            _taskRepo.FirstOrDefaultAsync(Arg.Any<Expression<Func<AgvTask, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<AgvTask?>(null));

            var input = new ForceEndTaskInput
            {
                TaskId = "NON-EXISTENT-TASK",
                Reason = "强制完结"
            };

            // Act & Assert
            var ex = await Should.ThrowAsync<EntityNotFoundException>(async () =>
            {
                await _appService.ForceEndTaskAsync(input);
            });
            ex.EntityType.ShouldBe(typeof(AgvTask));

            _opRecorder.Received(1).Record(Arg.Is<OperationLogContext>(ctx =>
                ctx.Action == "ForceEndTask" &&
                ctx.BeforeState == null &&
                ctx.AfterState == null
            ), OperationLogStatus.Failed, Arg.Any<string>());
        }

        [Fact]
        public async Task ForceEndTaskAsync_WhenTaskExists_Should_Succeed_And_RecordOperationLog()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var task = new AgvTask(taskId, "TASK-1002", "ST-01", "ST-02");
            task.Start(Guid.NewGuid(), "AGV-02", "TRACE-02");

            _taskRepo.FindAsync(Arg.Any<Expression<Func<AgvTask, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<AgvTask?>(task));

            var input = new ForceEndTaskInput
            {
                TaskId = "TASK-1002",
                AgvId = "AGV-02",
                Reason = "现场库位光电故障已人工搬移货物，强制完结"
            };

            // Act
            var result = await _appService.ForceEndTaskAsync(input);

            // Assert
            result.Success.ShouldBeTrue();
            result.CurrentState.ShouldBe("Succeeded");
            task.Status.ShouldBe(AgvTaskStatus.Succeeded);

            await _taskRepo.Received(1).UpdateAsync(task, autoSave: true);

            _opRecorder.Received(1).Record(Arg.Is<OperationLogContext>(ctx =>
                ctx.Module == "Dispatch" &&
                ctx.Action == "ForceEndTask" &&
                ctx.TargetId == "TASK-1002" &&
                ctx.TaskId == "TASK-1002" &&
                ctx.AgvId == "AGV-02" &&
                ctx.BeforeState == "Running" &&
                ctx.AfterState == "Succeeded" &&
                ctx.Reason == input.Reason
            ), OperationLogStatus.Success, null);
        }

        [Fact]
        public async Task ResetVehicleAsync_WhenVehicleNotFoundInRepo_Should_RecordFailure_And_ThrowEntityNotFound()
        {
            // Arrange
            _vehicleRepo.FindAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<AgvVehicle?>(null));
            _vehicleRepo.FirstOrDefaultAsync(Arg.Any<Expression<Func<AgvVehicle, bool>>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<AgvVehicle?>(null));

            var input = new ResetVehicleInput
            {
                AgvId = "AGV-UNKNOWN",
                Reason = "复位"
            };

            // Act & Assert
            var ex = await Should.ThrowAsync<EntityNotFoundException>(async () =>
            {
                await _appService.ResetVehicleAsync(input);
            });
            ex.EntityType.ShouldBe(typeof(AgvVehicle));

            _opRecorder.Received(1).Record(Arg.Is<OperationLogContext>(ctx =>
                ctx.Action == "ResetVehicle" &&
                ctx.BeforeState == null &&
                ctx.AfterState == null
            ), OperationLogStatus.Failed, Arg.Any<string>());
        }

        [Fact]
        public async Task ResetVehicleAsync_WhenVehicleExists_Should_Succeed_And_RecordOperationLog()
        {
            // Arrange
            var vehicleId = Guid.NewGuid();
            var vehicle = new AgvVehicle(vehicleId, "AGV-05");
            vehicle.ReportError("雷达避障触发超时");

            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<AgvVehicle, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<AgvVehicle?>(vehicle));

            var input = new ResetVehicleInput
            {
                AgvId = "AGV-05",
                Reason = "现场避障传感器清洁完毕，人工解除告警复位"
            };

            // Act
            var result = await _appService.ResetVehicleAsync(input);

            // Assert
            result.Success.ShouldBeTrue();
            result.TargetType.ShouldBe("Vehicle");
            result.TargetId.ShouldBe("AGV-05");
            result.CurrentState.ShouldBe("Idle");
            vehicle.Status.ShouldBe(VehicleStatus.Idle);

            await _vehicleRepo.Received(1).UpdateAsync(vehicle, autoSave: true);

            _opRecorder.Received(1).Record(Arg.Is<OperationLogContext>(ctx =>
                ctx.Module == "Dispatch" &&
                ctx.Action == "ResetVehicle" &&
                ctx.TargetType == "Vehicle" &&
                ctx.TargetId == "AGV-05" &&
                ctx.AgvId == "AGV-05" &&
                ctx.BeforeState == "Error" &&
                ctx.AfterState == "Idle" &&
                ctx.Reason == input.Reason
            ), OperationLogStatus.Success, null);
        }

        [Fact]
        public async Task AssignVehicleAsync_WhenEntitiesExist_Should_Bind_And_RecordOperationLog()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var task = new AgvTask(taskId, "TASK-1003", "ST-01", "ST-02");

            var vehicleId = Guid.NewGuid();
            var vehicle = new AgvVehicle(vehicleId, "AGV-03");

            _taskRepo.FindAsync(Arg.Any<Expression<Func<AgvTask, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<AgvTask?>(task));
            _vehicleRepo.FindAsync(Arg.Any<Expression<Func<AgvVehicle, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<AgvVehicle?>(vehicle));

            var input = new AssignVehicleInput
            {
                TaskId = "TASK-1003",
                AgvId = "AGV-03",
                Reason = "原分配车辆电量不足，手动改派备用车辆"
            };

            // Act
            var result = await _appService.AssignVehicleAsync(input);

            // Assert
            result.Success.ShouldBeTrue();
            result.CurrentState.ShouldBe("Assigned:AGV-03");
            task.AssignedVehicleCode.ShouldBe("AGV-03");
            vehicle.CurrentTaskCode.ShouldBe("TASK-1003");

            await _taskRepo.Received(1).UpdateAsync(task, autoSave: true);
            await _vehicleRepo.Received(1).UpdateAsync(vehicle, autoSave: true);

            _opRecorder.Received(1).Record(Arg.Is<OperationLogContext>(ctx =>
                ctx.Module == "Dispatch" &&
                ctx.Action == "AssignVehicle" &&
                ctx.TargetId == "TASK-1003" &&
                ctx.TaskId == "TASK-1003" &&
                ctx.AgvId == "AGV-03" &&
                ctx.BeforeState == "Unassigned" &&
                ctx.AfterState == "Assigned:AGV-03" &&
                ctx.Reason == input.Reason
            ), OperationLogStatus.Success, null);
        }

        [Fact]
        public async Task ResumeTaskAsync_Should_Resume_Failed_Task_And_Record_Audit()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var task = new AgvTask(taskId, "TASK-RESUME-01", "ST-01", "ST-02");
            task.Start(Guid.NewGuid(), "AGV-01");
            task.Fail("传感器超时告警");

            _taskRepo.FindAsync(Arg.Any<Expression<Func<AgvTask, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<AgvTask?>(task));

            var input = new ResumeTaskInput
            {
                TaskId = "TASK-RESUME-01",
                Reason = "现场传感器已人工复位，断点恢复步进",
                RetryCurrentStep = true
            };

            // Act
            var result = await _appService.ResumeTaskAsync(input);

            // Assert
            result.Success.ShouldBeTrue();
            result.BeforeState.ShouldBe("Failed[Step=1]");
            result.CurrentState.ShouldBe("Running[Step=1]");
            task.Status.ShouldBe(AgvTaskStatus.Running);
            task.FailureReason.ShouldBeNull();

            await _taskRepo.Received(1).UpdateAsync(task, autoSave: true);

            _opRecorder.Received(1).Record(Arg.Is<OperationLogContext>(ctx =>
                ctx.Module == "Dispatch" &&
                ctx.Action == "ResumeTask" &&
                ctx.TargetId == "TASK-RESUME-01" &&
                ctx.BeforeState == "Failed[Step=1]" &&
                ctx.AfterState == "Running[Step=1]" &&
                ctx.Reason == input.Reason
            ), OperationLogStatus.Success, null);
        }

        [Fact]
        public async Task RollbackAndRetryAsync_Should_Rollback_To_Target_Step_And_Record_Audit()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var task = new AgvTask(taskId, "TASK-ROLLBACK-01", "ST-01", "ST-02");
            task.Start(Guid.NewGuid(), "AGV-01");
            task.AdvanceStep(4, "Put");

            _taskRepo.FindAsync(Arg.Any<Expression<Func<AgvTask, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<AgvTask?>(task));

            var input = new RollbackAndRetryInput
            {
                TaskId = "TASK-ROLLBACK-01",
                TargetStepIndex = 1,
                Reason = "放置工位姿态校验偏差，安全回滚至对位步骤重新执行"
            };

            // Act
            var result = await _appService.RollbackAndRetryAsync(input);

            // Assert
            result.Success.ShouldBeTrue();
            result.BeforeState.ShouldBe("Running[Step=4]");
            result.CurrentState.ShouldBe("Running[Step=1]");
            task.Status.ShouldBe(AgvTaskStatus.Running);
            task.StepIndex.ShouldBe(1);

            await _taskRepo.Received(1).UpdateAsync(task, autoSave: true);

            _opRecorder.Received(1).Record(Arg.Is<OperationLogContext>(ctx =>
                ctx.Module == "Dispatch" &&
                ctx.Action == "RollbackAndRetry" &&
                ctx.TargetId == "TASK-ROLLBACK-01" &&
                ctx.BeforeState == "Running[Step=4]" &&
                ctx.AfterState == "Running[Step=1]" &&
                ctx.Reason == input.Reason
            ), OperationLogStatus.Success, null);
        }
    }
}
