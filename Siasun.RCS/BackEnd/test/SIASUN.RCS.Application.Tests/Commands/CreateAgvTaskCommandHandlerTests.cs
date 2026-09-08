using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Commands;
using SIASUN.RCS.Commands.Tasks;
using SIASUN.RCS.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Xunit;

namespace SIASUN.RCS.Application.Tests.Commands
{
    /// <summary>
    /// CQRS 创建调度任务命令处理器单元测试
    /// 验证任务创建的原子性、幂等防重、Audit 契约与 DTO 投影
    /// </summary>
    public class CreateAgvTaskCommandHandlerTests
    {
        private readonly IRepository<AgvTask, Guid> _taskRepository;
        private readonly IGuidGenerator _guidGenerator;
        private readonly CreateAgvTaskCommandHandler _handler;

        public CreateAgvTaskCommandHandlerTests()
        {
            _taskRepository = Substitute.For<IRepository<AgvTask, Guid>>();
            _guidGenerator = Substitute.For<IGuidGenerator>();
            _guidGenerator.Create().Returns(_ => Guid.NewGuid());
            _handler = new CreateAgvTaskCommandHandler(_taskRepository, _guidGenerator, NullLogger<CreateAgvTaskCommandHandler>.Instance);
        }

        [Fact]
        public async Task Handle_Should_Create_Task_Successfully()
        {
            var command = new CreateAgvTaskCommand(
                TaskCode: "TASK_CQRS_001",
                FromStation: "STATION_PICK",
                ToStation: "STATION_DROP",
                CarrierCode: "FOUP_88",
                BatchId: "BATCH_99",
                WorkflowKey: "erack_docking",
                TraceId: "TRACE_CQRS_01");

            _taskRepository.FindAsync(Arg.Any<Expression<Func<AgvTask, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<AgvTask?>(null));

            var result = await _handler.Handle(command, CancellationToken.None);

            result.ShouldNotBeNull();
            result.TaskCode.ShouldBe("TASK_CQRS_001");
            result.FromStation.ShouldBe("STATION_PICK");
            result.ToStation.ShouldBe("STATION_DROP");
            result.CarrierCode.ShouldBe("FOUP_88");
            result.BatchId.ShouldBe("BATCH_99");
            result.WorkflowKey.ShouldBe("erack_docking");
            result.TraceId.ShouldBe("TRACE_CQRS_01");

            await _taskRepository.Received(1).InsertAsync(Arg.Is<AgvTask>(t =>
                t.TaskCode == "TASK_CQRS_001" &&
                t.WorkflowKey == "erack_docking" &&
                t.CarrierCode == "FOUP_88"), true, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_Should_Throw_When_TaskCode_Already_Exists()
        {
            var command = new CreateAgvTaskCommand("TASK_EXISTING", "A", "B");

            var existingTask = new AgvTask(Guid.NewGuid(), "TASK_EXISTING", "A", "B");
            _taskRepository.FindAsync(Arg.Any<Expression<Func<AgvTask, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<AgvTask?>(existingTask));

            var ex = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _handler.Handle(command, CancellationToken.None);
            });

            ex.Code.ShouldBe("TASK_ALREADY_EXISTS");
            await _taskRepository.DidNotReceive().InsertAsync(Arg.Any<AgvTask>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void CreateAgvTaskCommand_Should_Expose_Audit_Metadata()
        {
            var command = new CreateAgvTaskCommand(
                TaskCode: "TASK_AUDIT_CHECK",
                FromStation: "LOC_A",
                ToStation: "LOC_B",
                CarrierCode: "C_01",
                TraceId: "TRC_123");

            var auditable = command as IAuditableCommand;
            auditable.ShouldNotBeNull();
            auditable.Module.ShouldBe("TaskDispatch");
            auditable.Action.ShouldBe("CreateTask");
            auditable.TargetType.ShouldBe("Task");
            auditable.TargetId.ShouldBe("TASK_AUDIT_CHECK");
            auditable.Description.ShouldNotBeNull();
            auditable.Description.ShouldContain("LOC_A");
            auditable.Description.ShouldContain("LOC_B");
        }
    }
}

