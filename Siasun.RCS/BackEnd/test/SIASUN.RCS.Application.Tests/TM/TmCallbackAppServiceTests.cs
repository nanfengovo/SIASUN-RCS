using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Tasks;
using SIASUN.RCS.Tasks.Workflow;
using SIASUN.RCS.TM;
using SIASUN.RCS.TM.Dtos;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace SIASUN.RCS.Application.Tests.TM
{
    /// <summary>
    /// TM 报文回调与下发映射单元测试（验证零 hack 字符串映射、工作流唤醒与生命周期驱动）
    /// </summary>
    public class TmCallbackAppServiceTests
    {
        private readonly ITaskSerialRegistry _serialRegistry;
        private readonly IRepository<AgvTask, Guid> _taskRepo;
        private readonly ITaskWorkflowEngine _workflowEngine;
        private readonly ILogger<TmCallbackAppService> _logger;
        private readonly TmCallbackAppService _appService;

        public TmCallbackAppServiceTests()
        {
            _serialRegistry = Substitute.For<ITaskSerialRegistry>();
            _taskRepo = Substitute.For<IRepository<AgvTask, Guid>>();
            _workflowEngine = Substitute.For<ITaskWorkflowEngine>();
            _logger = Substitute.For<ILogger<TmCallbackAppService>>();

            _appService = new TmCallbackAppService(_serialRegistry, _taskRepo, _workflowEngine, _logger);
        }

        [Fact]
        public async Task Should_Handle_Completed_Callback_And_Resume_Workflow()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var task = new AgvTask(taskId, "TASK-TM-01", "LOC-01", "LOC-02");
            task.Start(Guid.NewGuid(), "AGV-01");
            task.AdvanceStep(1, "Fetch", "TM_FETCH_DONE");

            var tmSerial = "TM_20260908_FETCH_9999";
            var mapping = new TaskSerialMapping(
                Guid.NewGuid(), taskId, "TASK-TM-01", tmSerial, "Fetch", 1, "TM_FETCH_DONE", "AGV-01");

            _serialRegistry.FindByTmSerialAsync(tmSerial, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<TaskSerialMapping?>(mapping));
            _taskRepo.FindAsync(taskId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<AgvTask?>(task));

            var input = new TmCallbackInput
            {
                TmSerial = tmSerial,
                Status = "Completed",
                VehicleCode = "AGV-01",
                Payload = "{\"motionDurationMs\": 3500}"
            };

            // Act
            var result = await _appService.HandleCallbackAsync(input);

            // Assert
            result.Success.ShouldBeTrue();
            result.TaskCode.ShouldBe("TASK-TM-01");
            result.Leg.ShouldBe("Fetch");

            // 验证唤醒工作流引擎
            await _workflowEngine.Received(1).ResumeBySignalAsync(
                task,
                "TM_FETCH_DONE",
                Arg.Any<object?>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());

            await _taskRepo.Received(1).UpdateAsync(task, autoSave: true);
        }

        [Fact]
        public async Task Should_Handle_Failed_Callback_And_Fail_Task()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var task = new AgvTask(taskId, "TASK-TM-02", "LOC-01", "LOC-02");
            task.Start(Guid.NewGuid(), "AGV-02");
            task.AdvanceStep(1, "Fetch", "TM_FETCH_DONE");

            var tmSerial = "TM_FAIL_001";
            var mapping = new TaskSerialMapping(
                Guid.NewGuid(), taskId, "TASK-TM-02", tmSerial, "Fetch", 1, "TM_FETCH_DONE", "AGV-02");

            _serialRegistry.FindByTmSerialAsync(tmSerial, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<TaskSerialMapping?>(mapping));
            _taskRepo.FindAsync(taskId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<AgvTask?>(task));

            var input = new TmCallbackInput
            {
                TmSerial = tmSerial,
                Status = "Failed",
                ErrorCode = "ERR_GRIPPER_TIMEOUT",
                ErrorMessage = "晶圆夹爪抓取超时"
            };

            // Act
            var result = await _appService.HandleCallbackAsync(input);

            // Assert
            result.Success.ShouldBeTrue();
            task.Status.ShouldBe(AgvTaskStatus.Failed);
            task.FailureReason.ShouldNotBeNull();
            task.FailureReason.ShouldContain("晶圆夹爪抓取超时");

            await _taskRepo.Received(1).UpdateAsync(task, autoSave: true);
        }

        [Fact]
        public async Task Should_Return_Error_When_TmSerial_Not_Found()
        {
            // Arrange
            _serialRegistry.FindByTmSerialAsync("NON_EXISTENT_TM", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<TaskSerialMapping?>(null));

            var input = new TmCallbackInput
            {
                TmSerial = "NON_EXISTENT_TM",
                Status = "Completed"
            };

            // Act
            var result = await _appService.HandleCallbackAsync(input);

            // Assert
            result.Success.ShouldBeFalse();
            result.Message.ShouldContain("未找到 TM 序列号");
        }

        [Fact]
        public async Task OutboundAdapter_Should_Register_Serial_And_Return_Result()
        {
            // Arrange
            var mockLogger = Substitute.For<ILogger<MockTmOutboundAdapter>>();
            var adapter = new MockTmOutboundAdapter(_serialRegistry, mockLogger);

            var ctx = new TmDispatchContext(
                Guid.NewGuid(),
                "TASK-DISPATCH-01",
                "Fetch",
                1,
                "AGV-01",
                optionCode: "0x00010002",
                targetStation: "ST-01",
                waitingEvent: "TM_FETCH_DONE",
                traceId: "TRACE-001");

            // Act
            var dispatchResult = await adapter.DispatchLegAsync(ctx);

            // Assert
            dispatchResult.Success.ShouldBeTrue();
            dispatchResult.TmSerial.ShouldNotBeNullOrWhiteSpace();
            dispatchResult.TmSerial.ShouldContain("FETCH");

            await _serialRegistry.Received(1).RegisterAsync(
                ctx.TaskId,
                ctx.TaskCode,
                dispatchResult.TmSerial,
                ctx.Leg,
                ctx.StepIndex,
                ctx.WaitingEvent,
                ctx.VehicleCode,
                Arg.Any<CancellationToken>());
        }
    }
}
