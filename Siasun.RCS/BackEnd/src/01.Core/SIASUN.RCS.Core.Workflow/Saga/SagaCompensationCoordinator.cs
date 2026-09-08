using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Tasks.Workflow.Saga
{
    /// <summary>
    /// SAGA 四级逆向事务补偿器默认实现
    /// </summary>
    public class SagaCompensationCoordinator : ISagaCompensationCoordinator, ITransientDependency
    {
        private readonly ITaskWorkflowEngine _workflowEngine;
        private readonly ILogger<SagaCompensationCoordinator> _logger;

        public SagaCompensationCoordinator(
            ITaskWorkflowEngine workflowEngine,
            ILogger<SagaCompensationCoordinator> logger)
        {
            _workflowEngine = workflowEngine;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<bool> CompensateAsync(
            IWorkflowTask task,
            SagaCompensationLevel level,
            int targetStepIndex = 0,
            string reason = "异常自动补偿",
            CancellationToken cancellationToken = default)
        {
            Check.NotNull(task, nameof(task));

            _logger.LogWarning("任务 [{TaskCode}] 正在执行 SAGA 补偿 [Level: {Level}], 原因: {Reason}",
                task.TaskCode, level, reason);

            switch (level)
            {
                case SagaCompensationLevel.Level1_RetryCurrentStep:
                    // 原地重试：由外部重试策略或当前步骤再次步进触发
                    task.RecordRetry($"Step_{task.StepIndex}", reason, isIdempotent: true);
                    return true;

                case SagaCompensationLevel.Level2_RollbackToSafePoint:
                    // 倒序步退：回退至基准安全步
                    var rollbackSuccess = await _workflowEngine.RollbackToStepAsync(
                        task,
                        targetStepIndex,
                        reason,
                        cancellationToken: cancellationToken);
                    return rollbackSuccess;

                case SagaCompensationLevel.Level3_ReleaseResources:
                    // 跨系统资源逆向释放，并退回待命状态
                    task.RollbackToStep(targetStepIndex, $"释放资源补偿: {reason}");
                    _logger.LogInformation("任务 [{TaskCode}] SAGA Level 3 资源回滚释放完成。", task.TaskCode);
                    return true;

                case SagaCompensationLevel.Level4_NotifyOperatorAndFail:
                default:
                    // 终态隔离：不可恢复物理异常，置为 Failed 并上报
                    task.Fail($"SAGA Level 4 严重故障熔断: {reason}");
                    _logger.LogError("任务 [{TaskCode}] SAGA 补偿判定不可自动恢复，已置为 Failed 并呼叫人工介入。", task.TaskCode);
                    return false;
            }
        }
    }
}
