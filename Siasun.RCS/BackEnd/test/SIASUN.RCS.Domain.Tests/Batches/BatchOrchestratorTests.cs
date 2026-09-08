using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Xunit;

namespace SIASUN.RCS.Batches
{
    /// <summary>
    /// 批次管理与多车协同编排领域服务单元测试
    /// 验证《AGENTS.md》铁律 6 批次分解、载具绑定与汇聚同步逻辑
    /// </summary>
    public class BatchOrchestratorTests
    {
        private readonly IRepository<AgvTask, Guid> _taskRepository;
        private readonly IGuidGenerator _guidGenerator;
        private readonly BatchOrchestrator _orchestrator;

        public BatchOrchestratorTests()
        {
            _taskRepository = Substitute.For<IRepository<AgvTask, Guid>>();
            _guidGenerator = Substitute.For<IGuidGenerator>();
            _guidGenerator.Create().Returns(_ => Guid.NewGuid());
            _orchestrator = new BatchOrchestrator(_taskRepository, _guidGenerator, NullLogger<BatchOrchestrator>.Instance);
        }

        [Fact]
        public async Task DecomposeBatchAsync_Should_Throw_When_CarrierCodes_Empty()
        {
            var batch = new AgvBatch(Guid.NewGuid(), "BATCH_EMPTY", "STATION_A", "STATION_B");

            var ex = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await _orchestrator.DecomposeBatchAsync(batch, Array.Empty<string>());
            });

            ex.Code.ShouldBe("BATCH_NO_CARRIERS");
        }

        [Fact]
        public async Task DecomposeBatchAsync_Should_Create_SubTasks_And_Configure_Batch()
        {
            var batch = new AgvBatch(Guid.NewGuid(), "BATCH_001", "STATION_A", "STATION_B", traceId: "TRC_001");
            var carriers = new[] { "FOUP_A", "FOUP_B" };

            var tasks = await _orchestrator.DecomposeBatchAsync(batch, carriers, "transfer_standard");

            tasks.Count.ShouldBe(2);
            tasks[0].TaskCode.ShouldBe("BATCH_001_01_FOUP_A");
            tasks[0].CarrierCode.ShouldBe("FOUP_A");
            tasks[0].FromStation.ShouldBe("STATION_A");
            tasks[0].ToStation.ShouldBe("STATION_B");
            tasks[0].WorkflowKey.ShouldBe("transfer_standard");
            tasks[0].TraceId.ShouldBe("TRC_001");

            tasks[1].TaskCode.ShouldBe("BATCH_001_02_FOUP_B");
            tasks[1].CarrierCode.ShouldBe("FOUP_B");

            batch.Status.ShouldBe(AgvBatchStatus.Dispatching);
            batch.TotalSubTasks.ShouldBe(2);
            batch.CarrierCodes.ShouldBe("FOUP_A,FOUP_B");

            await _taskRepository.Received(2).InsertAsync(Arg.Any<AgvTask>(), true, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task EvaluateConvergenceAsync_Should_Return_True_When_All_SubTasks_Succeeded()
        {
            var batch = new AgvBatch(Guid.NewGuid(), "BATCH_002", "STATION_A", "STATION_B");
            batch.ConfigureSubTasks(2, "FOUP_1,FOUP_2");

            var task1 = new AgvTask(Guid.NewGuid(), "BATCH_002_01", "STATION_A", "STATION_B", carrierCode: "FOUP_1", batchId: "BATCH_002");
            task1.Start(Guid.NewGuid(), "AGV_01");
            task1.Complete();

            var task2 = new AgvTask(Guid.NewGuid(), "BATCH_002_02", "STATION_A", "STATION_B", carrierCode: "FOUP_2", batchId: "BATCH_002");
            task2.Start(Guid.NewGuid(), "AGV_02");
            task2.Complete();

            _taskRepository.GetListAsync(Arg.Any<Expression<Func<AgvTask, bool>>>(), cancellationToken: Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<List<AgvTask>>(new List<AgvTask> { task1, task2 }));

            var result = await _orchestrator.EvaluateConvergenceAsync(batch);

            result.ShouldBeTrue();
            batch.Status.ShouldBe(AgvBatchStatus.Completed);
            batch.CompletedSubTasks.ShouldBe(2);
            batch.FailedSubTasks.ShouldBe(0);
        }

        [Fact]
        public async Task EvaluateConvergenceAsync_Should_Return_False_And_Mark_Failed_When_Any_Task_Failed()
        {
            var batch = new AgvBatch(Guid.NewGuid(), "BATCH_003", "STATION_A", "STATION_B");
            batch.ConfigureSubTasks(2, "FOUP_1,FOUP_2");

            var task1 = new AgvTask(Guid.NewGuid(), "BATCH_003_01", "STATION_A", "STATION_B", carrierCode: "FOUP_1", batchId: "BATCH_003");
            task1.Start(Guid.NewGuid(), "AGV_01");
            task1.Complete();

            var task2 = new AgvTask(Guid.NewGuid(), "BATCH_003_02", "STATION_A", "STATION_B", carrierCode: "FOUP_2", batchId: "BATCH_003");
            task2.Start(Guid.NewGuid(), "AGV_02");
            task2.Fail("Vehicle breakdown");

            _taskRepository.GetListAsync(Arg.Any<Expression<Func<AgvTask, bool>>>(), cancellationToken: Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<List<AgvTask>>(new List<AgvTask> { task1, task2 }));

            var result = await _orchestrator.EvaluateConvergenceAsync(batch);

            result.ShouldBeFalse();
            batch.Status.ShouldBe(AgvBatchStatus.Failed);
            batch.FailedSubTasks.ShouldBe(1);
        }

        [Fact]
        public void AgvBatch_Cancel_Should_Transition_To_Canceled()
        {
            var batch = new AgvBatch(Guid.NewGuid(), "BATCH_004", "STATION_A", "STATION_B");
            batch.Cancel("Manual cancel by operator");

            batch.Status.ShouldBe(AgvBatchStatus.Canceled);
            batch.Remark.ShouldNotBeNull();
            batch.Remark.ShouldContain("Manual cancel by operator");
        }
    }
}

