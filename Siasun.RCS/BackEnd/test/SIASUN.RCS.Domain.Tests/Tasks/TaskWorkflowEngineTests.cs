using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using SIASUN.RCS.Tasks.Events;
using Volo.Abp;
using Xunit;

namespace SIASUN.RCS.Tasks.Workflow
{
    public class TaskWorkflowEngineTests
    {
        private class TestStep : WorkflowStepBase
        {
            public override int StepIndex { get; }
            public override string StepName { get; }
            public override bool IsIdempotent { get; }
            public Func<WorkflowStepContext, Task<StepExecutionResult>> ExecuteFunc { get; set; }
            public Func<WorkflowStepContext, Task>? CompensateFunc { get; set; }

            public TestStep(int stepIndex, string stepName, bool isIdempotent, Func<WorkflowStepContext, Task<StepExecutionResult>> executeFunc)
            {
                StepIndex = stepIndex;
                StepName = stepName;
                IsIdempotent = isIdempotent;
                ExecuteFunc = executeFunc;
            }

            public override Task<StepExecutionResult> ExecuteAsync(WorkflowStepContext context) => ExecuteFunc(context);

            public override Task CompensateAsync(WorkflowStepContext context)
            {
                return CompensateFunc != null ? CompensateFunc(context) : base.CompensateAsync(context);
            }
        }

        private readonly TaskWorkflowEngine _engine;

        public TaskWorkflowEngineTests()
        {
            _engine = new TaskWorkflowEngine(null!, NullLogger<TaskWorkflowEngine>.Instance);
        }

        [Fact]
        public async Task Should_Advance_Steps_Through_Success()
        {
            var steps = new[]
            {
                new TestStep(1, "Step1_Check", true, _ => Task.FromResult(StepExecutionResult.Success())),
                new TestStep(2, "Step2_Fetch", false, _ => Task.FromResult(StepExecutionResult.Success()))
            };

            _engine.RegisterWorkflow("TestPipeline", steps);

            var task = new AgvTask(Guid.NewGuid(), "TASK_001");
            task.Start(Guid.NewGuid(), "AGV_01");

            task.StepIndex.ShouldBe(1);
            task.Status.ShouldBe(AgvTaskStatus.Running);

            // 执行第 1 步 -> 推进到第 2 步
            var res1 = await _engine.ExecuteCurrentStepAsync(task, "TestPipeline");
            res1.Status.ShouldBe(StepStatus.Success);
            task.StepIndex.ShouldBe(2);
            task.Status.ShouldBe(AgvTaskStatus.Running);

            // 执行第 2 步 -> 最后一步，任务完成
            var res2 = await _engine.ExecuteCurrentStepAsync(task, "TestPipeline");
            res2.Status.ShouldBe(StepStatus.Success);
            task.Status.ShouldBe(AgvTaskStatus.Succeeded);
            task.EndTime.ShouldNotBeNull();
        }

        [Fact]
        public async Task Should_Suspend_And_Resume_By_Matching_Signal()
        {
            var steps = new[]
            {
                new TestStep(1, "Step1_SendCmd", true, _ => Task.FromResult(StepExecutionResult.Suspend("TM_FETCH_DONE"))),
                new TestStep(2, "Step2_Complete", true, _ => Task.FromResult(StepExecutionResult.Success()))
            };

            _engine.RegisterWorkflow("SuspendResume", steps);

            var task = new AgvTask(Guid.NewGuid(), "TASK_002");
            task.Start(Guid.NewGuid(), "AGV_01");

            // 执行第 1 步 -> 挂起
            var res = await _engine.ExecuteCurrentStepAsync(task, "SuspendResume");
            res.Status.ShouldBe(StepStatus.Suspended);
            task.WaitingEvent.ShouldBe("TM_FETCH_DONE");

            // 外部错误信号唤醒失败
            var mismatchRes = await _engine.ResumeBySignalAsync(task, "WRONG_SIGNAL", workflowKey: "SuspendResume");
            mismatchRes.Status.ShouldBe(StepStatus.Failed);
            task.WaitingEvent.ShouldBe("TM_FETCH_DONE");

            // 正确信号唤醒 -> 推进到第 2 步
            var resumeRes = await _engine.ResumeBySignalAsync(task, "TM_FETCH_DONE", workflowKey: "SuspendResume");
            resumeRes.Status.ShouldBe(StepStatus.Success);
            task.WaitingEvent.ShouldBeNull();
            task.StepIndex.ShouldBe(2);
        }

        [Fact]
        public async Task Should_Retry_When_Step_Is_Idempotent()
        {
            var attempts = 0;
            var steps = new[]
            {
                new TestStep(1, "Step1_QueryPlc", true, _ =>
                {
                    attempts++;
                    if (attempts == 1) return Task.FromResult(StepExecutionResult.Retry("PLC 通信超时"));
                    return Task.FromResult(StepExecutionResult.Success());
                })
            };

            _engine.RegisterWorkflow("IdempotentRetry", steps);

            var task = new AgvTask(Guid.NewGuid(), "TASK_003");
            task.Start(Guid.NewGuid(), "AGV_01");

            // 第一次执行 -> 返回 Retry
            var res1 = await _engine.ExecuteCurrentStepAsync(task, "IdempotentRetry");
            res1.Status.ShouldBe(StepStatus.Retry);
            task.Status.ShouldBe(AgvTaskStatus.Running); // 保持 Running
            task.StepIndex.ShouldBe(1); // StepIndex 不动
            task.RetryCount.ShouldBe(1);

            // 第二次执行 -> 成功
            var res2 = await _engine.ExecuteCurrentStepAsync(task, "IdempotentRetry");
            res2.Status.ShouldBe(StepStatus.Success);
            task.Status.ShouldBe(AgvTaskStatus.Succeeded);
            task.RetryCount.ShouldBe(0); // 成功后重置
        }

        [Fact]
        public async Task Should_Fail_Immediately_When_Step_Is_Non_Idempotent()
        {
            var steps = new[]
            {
                new TestStep(1, "Step1_LiftCarrier", false, _ => Task.FromResult(StepExecutionResult.Retry("举升货叉异常")))
            };

            _engine.RegisterWorkflow("NonIdempotentFail", steps);

            var task = new AgvTask(Guid.NewGuid(), "TASK_004");
            task.Start(Guid.NewGuid(), "AGV_01");

            var res = await _engine.ExecuteCurrentStepAsync(task, "NonIdempotentFail");
            res.Status.ShouldBe(StepStatus.Retry);
            task.Status.ShouldBe(AgvTaskStatus.Failed); // 非幂等动作立即失败
            task.FailureReason.ShouldNotBeNull();
            task.FailureReason.ShouldContain("非幂等步骤");
        }

        [Fact]
        public async Task Should_Fail_When_Idempotent_Retries_Exceed_Limit()
        {
            var steps = new[]
            {
                new TestStep(1, "Step1_PlcHandshake", true, _ => Task.FromResult(StepExecutionResult.Retry("握手超时")))
            };

            _engine.RegisterWorkflow("ExceedRetryLimit", steps);

            var task = new AgvTask(Guid.NewGuid(), "TASK_005");
            task.Start(Guid.NewGuid(), "AGV_01");
            task.ConfigureMaxRetryCount(2);

            // 尝试 1
            await _engine.ExecuteCurrentStepAsync(task, "ExceedRetryLimit");
            task.Status.ShouldBe(AgvTaskStatus.Running);
            task.RetryCount.ShouldBe(1);

            // 尝试 2
            await _engine.ExecuteCurrentStepAsync(task, "ExceedRetryLimit");
            task.Status.ShouldBe(AgvTaskStatus.Running);
            task.RetryCount.ShouldBe(2);

            // 尝试 3 -> 超过最大 2 次，转为 Failed
            await _engine.ExecuteCurrentStepAsync(task, "ExceedRetryLimit");
            task.Status.ShouldBe(AgvTaskStatus.Failed);
            task.FailureReason.ShouldNotBeNull();
            task.FailureReason.ShouldContain("达到上限 (2 次)");
        }

        [Fact]
        public async Task Should_Support_Resume_From_Failure_And_SAGA_Rollback()
        {
            var compensated = false;
            var steps = new[]
            {
                new TestStep(1, "Step1_NavigateToWaitPoint", true, _ => Task.FromResult(StepExecutionResult.Success())),
                new TestStep(2, "Step2_FetchAction", false, _ => Task.FromResult(StepExecutionResult.Fail("机械臂卡阻")))
                {
                    CompensateFunc = _ =>
                    {
                        compensated = true;
                        return Task.CompletedTask;
                    }
                }
            };

            _engine.RegisterWorkflow("SagaWorkflow", steps);

            var task = new AgvTask(Guid.NewGuid(), "TASK_006");
            task.Start(Guid.NewGuid(), "AGV_01");

            // 执行第 1 步成功
            await _engine.ExecuteCurrentStepAsync(task, "SagaWorkflow");
            task.StepIndex.ShouldBe(2);

            // 执行第 2 步失败
            await _engine.ExecuteCurrentStepAsync(task, "SagaWorkflow");
            task.Status.ShouldBe(AgvTaskStatus.Failed);

            // 调度员人工显式恢复
            task.ResumeFromFailure("现场操作员手动排除异物，执行恢复");
            task.Status.ShouldBe(AgvTaskStatus.Running);
            task.FailureReason.ShouldBeNull();

            // 执行 SAGA 补偿步退至第 1 步
            var rollbackSuccess = await _engine.RollbackToStepAsync(task, 1, "回退至等待点重新对位", "SagaWorkflow");
            rollbackSuccess.ShouldBeTrue();
            compensated.ShouldBeTrue();
            task.StepIndex.ShouldBe(1);
            task.Status.ShouldBe(AgvTaskStatus.Running);
        }
    }
}
