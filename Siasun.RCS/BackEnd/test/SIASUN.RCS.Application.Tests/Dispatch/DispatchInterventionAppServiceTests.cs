using System;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Dispatch;
using SIASUN.RCS.Dispatch.Dtos;
using SIASUN.RCS.Interfaces.OperationLogs;
using SIASUN.RCS.Logs.OperatorLogs;
using Volo.Abp;
using Xunit;

namespace SIASUN.RCS.Application.Tests.Dispatch
{
    /// <summary>
    /// 调度员人工干预应用服务单元测试
    /// 验证任务取消、强制完结、人工指派车辆、车辆复位等关键干预操作均 100% 记录责任审计
    /// </summary>
    public class DispatchInterventionAppServiceTests
    {
        private readonly IOperationLogRecorder _opRecorder;
        private readonly DispatchInterventionAppService _appService;

        public DispatchInterventionAppServiceTests()
        {
            _opRecorder = Substitute.For<IOperationLogRecorder>();
            _appService = new DispatchInterventionAppService(_opRecorder);
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
        public async Task CancelTaskAsync_WithValidInput_Should_Succeed_And_RecordOperationLog()
        {
            // Arrange
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

            _opRecorder.Received(1).Record(Arg.Is<OperationLogContext>(ctx =>
                ctx.Module == "Dispatch" &&
                ctx.Action == "CancelTask" &&
                ctx.TargetId == "TASK-1001" &&
                ctx.TaskId == "TASK-1001" &&
                ctx.AgvId == "AGV-01" &&
                ctx.BeforeState == "Running" &&
                ctx.AfterState == "Canceled" &&
                ctx.Reason == input.Reason
            ));
        }

        [Fact]
        public async Task ForceEndTaskAsync_WithValidInput_Should_Succeed_And_RecordOperationLog()
        {
            // Arrange
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

            _opRecorder.Received(1).Record(Arg.Is<OperationLogContext>(ctx =>
                ctx.Module == "Dispatch" &&
                ctx.Action == "ForceEndTask" &&
                ctx.TargetId == "TASK-1002" &&
                ctx.TaskId == "TASK-1002" &&
                ctx.AgvId == "AGV-02" &&
                ctx.BeforeState == "Running" &&
                ctx.AfterState == "Succeeded" &&
                ctx.Reason == input.Reason
            ));
        }

        [Fact]
        public async Task AssignVehicleAsync_WithValidInput_Should_Succeed_And_RecordOperationLog()
        {
            // Arrange
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

            _opRecorder.Received(1).Record(Arg.Is<OperationLogContext>(ctx =>
                ctx.Module == "Dispatch" &&
                ctx.Action == "AssignVehicle" &&
                ctx.TargetId == "TASK-1003" &&
                ctx.TaskId == "TASK-1003" &&
                ctx.AgvId == "AGV-03" &&
                ctx.BeforeState == "Unassigned" &&
                ctx.AfterState == "Assigned:AGV-03" &&
                ctx.Reason == input.Reason
            ));
        }

        [Fact]
        public async Task ResetVehicleAsync_WithValidInput_Should_Succeed_And_RecordOperationLog()
        {
            // Arrange
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

            _opRecorder.Received(1).Record(Arg.Is<OperationLogContext>(ctx =>
                ctx.Module == "Dispatch" &&
                ctx.Action == "ResetVehicle" &&
                ctx.TargetType == "Vehicle" &&
                ctx.TargetId == "AGV-05" &&
                ctx.AgvId == "AGV-05" &&
                ctx.BeforeState == "Error" &&
                ctx.AfterState == "Idle" &&
                ctx.Reason == input.Reason
            ));
        }
    }
}

