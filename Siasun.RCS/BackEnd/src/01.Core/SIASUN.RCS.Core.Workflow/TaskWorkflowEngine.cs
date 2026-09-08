using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Tasks.Workflow
{
    /// <summary>
    /// 轻量步进式 TaskWorkflow 引擎默认实现
    /// 严禁使用巨型 DAG 或 22 状态机，严格由 StepIndex、WaitingEvent、ActiveLeg 驱动
    /// </summary>
    public class TaskWorkflowEngine : ITaskWorkflowEngine, ISingletonDependency
    {
        private readonly ConcurrentDictionary<string, List<IWorkflowStep>> _workflows = new(StringComparer.OrdinalIgnoreCase);
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TaskWorkflowEngine> _logger;

        public TaskWorkflowEngine(
            IServiceProvider serviceProvider,
            ILogger<TaskWorkflowEngine> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <inheritdoc />
        public void RegisterWorkflow(string workflowKey, IEnumerable<IWorkflowStep> steps)
        {
            Check.NotNullOrWhiteSpace(workflowKey, nameof(workflowKey));
            Check.NotNull(steps, nameof(steps));

            var ordered = steps.OrderBy(s => s.StepIndex).ToList();
            _workflows.AddOrUpdate(workflowKey, ordered, (_, _) => ordered);
            _logger.LogInformation("工作流 [{Key}] 成功注册 {Count} 个步进节点。", workflowKey, ordered.Count);
        }

        /// <inheritdoc />
        public IReadOnlyList<IWorkflowStep> GetSteps(string workflowKey)
        {
            if (_workflows.TryGetValue(workflowKey, out var steps))
            {
                return steps.AsReadOnly();
            }

            return Array.Empty<IWorkflowStep>();
        }

        /// <inheritdoc />
        public async Task<StepExecutionResult> ExecuteCurrentStepAsync(
            IWorkflowTask task,
            string workflowKey = "Default",
            CancellationToken cancellationToken = default)
        {
            Check.NotNull(task, nameof(task));

            if (task.Status != AgvTaskStatus.Running)
            {
                _logger.LogWarning("任务 [{TaskCode}] 当前状态为 {Status}，非 Running 状态不可步进。", task.TaskCode, task.Status);
                return StepExecutionResult.Fail($"任务处于非运行状态: {task.Status}");
            }

            if (!string.IsNullOrWhiteSpace(task.WaitingEvent))
            {
                _logger.LogInformation("任务 [{TaskCode}] 步骤 [{StepIndex}] 处于挂起等待事件 [{WaitingEvent}]，暂缓步进。",
                    task.TaskCode, task.StepIndex, task.WaitingEvent);
                return StepExecutionResult.Suspend(task.WaitingEvent);
            }

            if (!_workflows.TryGetValue(workflowKey, out var steps) || steps.Count == 0)
            {
                throw new BusinessException("RCS:WorkflowNotRegistered")
                    .WithData("WorkflowKey", workflowKey)
                    .WithData("TaskCode", task.TaskCode);
            }

            var currentStep = steps.FirstOrDefault(s => s.StepIndex == task.StepIndex);
            if (currentStep == null)
            {
                // 超出最大步进索引，视为整单成功完结
                var maxStep = steps.Max(s => s.StepIndex);
                if (task.StepIndex > maxStep)
                {
                    task.Complete();
                    _logger.LogInformation("任务 [{TaskCode}] 所有工作流步骤执行完毕，正常完结 (Succeeded)。", task.TaskCode);
                    return StepExecutionResult.Success(task.StepIndex, "所有工作流步骤已完成");
                }

                task.Fail($"未找到序号为 {task.StepIndex} 的步骤定义");
                return StepExecutionResult.Fail($"未定义步骤 {task.StepIndex}");
            }

            var context = new WorkflowStepContext(task, _serviceProvider, cancellationToken);

            try
            {
                var result = await currentStep.ExecuteAsync(context);

                switch (result.Status)
                {
                    case StepStatus.Success:
                        var nextStepIndex = result.NextStepIndex ?? (task.StepIndex + 1);
                        var isLastStep = !steps.Any(s => s.StepIndex >= nextStepIndex);

                        if (isLastStep)
                        {
                            task.Complete();
                            _logger.LogInformation("任务 [{TaskCode}] 执行最后一步 [{StepName}] 成功，任务完结。", task.TaskCode, currentStep.StepName);
                        }
                        else
                        {
                            var nextStepDef = steps.FirstOrDefault(s => s.StepIndex == nextStepIndex);
                            task.AdvanceStep(nextStepIndex, nextStepDef?.ActiveLeg ?? currentStep.ActiveLeg);
                            _logger.LogInformation("任务 [{TaskCode}] 步骤 [{Current}] -> [{Next}] 推进成功 (程段: {Leg})。",
                                task.TaskCode, currentStep.StepName, nextStepDef?.StepName ?? nextStepIndex.ToString(), task.ActiveLeg);
                        }
                        return result;

                    case StepStatus.Suspended:
                        task.Suspend(result.WaitingEvent!);
                        _logger.LogInformation("任务 [{TaskCode}] 步骤 [{StepName}] 挂起，等待信号: {Event}",
                            task.TaskCode, currentStep.StepName, result.WaitingEvent);
                        return result;

                    case StepStatus.Retry:
                        task.RecordRetry(currentStep.StepName, result.Message ?? "步骤瞬态异常请求重试", currentStep.IsIdempotent);
                        _logger.LogWarning("任务 [{TaskCode}] 步骤 [{StepName}] 触发重试 (第 {Count}/{Max} 次, 幂等: {Idempotent}): {Reason}",
                            task.TaskCode, currentStep.StepName, task.RetryCount, task.MaxRetryCount, currentStep.IsIdempotent, result.Message);
                        return result;

                    case StepStatus.Failed:
                    default:
                        task.Fail(result.Message ?? $"步骤 [{currentStep.StepName}] 执行失败");
                        _logger.LogError("任务 [{TaskCode}] 步骤 [{StepName}] 失败: {Reason}", task.TaskCode, currentStep.StepName, result.Message);
                        return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "任务 [{TaskCode}] 步骤 [{StepName}] 执行抛出未捕获异常: {Message}", task.TaskCode, currentStep.StepName, ex.Message);
                if (currentStep.IsIdempotent)
                {
                    task.RecordRetry(currentStep.StepName, ex.Message, true);
                    return StepExecutionResult.Retry(ex.Message);
                }
                else
                {
                    task.Fail($"非幂等步骤 [{currentStep.StepName}] 异常，立即置为失败: {ex.Message}");
                    return StepExecutionResult.Fail(ex.Message);
                }
            }
        }

        /// <inheritdoc />
        public async Task<StepExecutionResult> ResumeBySignalAsync(
            IWorkflowTask task,
            string signalEvent,
            object? payload = null,
            string workflowKey = "Default",
            CancellationToken cancellationToken = default)
        {
            Check.NotNull(task, nameof(task));
            Check.NotNullOrWhiteSpace(signalEvent, nameof(signalEvent));

            if (task.Status != AgvTaskStatus.Running)
            {
                _logger.LogWarning("任务 [{TaskCode}] 处于 {Status} 状态，忽略唤醒信号: {Event}", task.TaskCode, task.Status, signalEvent);
                return StepExecutionResult.Fail($"任务处于非运行状态: {task.Status}");
            }

            if (!string.Equals(task.WaitingEvent, signalEvent, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("任务 [{TaskCode}] 等待信号 [{Expected}] 与接收信号 [{Received}] 不匹配，忽略。",
                    task.TaskCode, task.WaitingEvent, signalEvent);
                return StepExecutionResult.Fail($"信号不匹配: 期待 [{task.WaitingEvent}], 收到 [{signalEvent}]");
            }

            // 成功唤醒，解除挂起
            task.ResumeByEvent(signalEvent);
            _logger.LogInformation("任务 [{TaskCode}] 步骤 [{StepIndex}] 成功由信号 [{Event}] 唤醒。",
                task.TaskCode, task.StepIndex, signalEvent);

            // 推进到下一步或继续执行
            if (_workflows.TryGetValue(workflowKey, out var steps))
            {
                var currentStep = steps.FirstOrDefault(s => s.StepIndex == task.StepIndex);
                var nextStepIndex = task.StepIndex + 1;
                var isLastStep = !steps.Any(s => s.StepIndex >= nextStepIndex);

                if (isLastStep)
                {
                    task.Complete();
                    return StepExecutionResult.Success(task.StepIndex, "收到最终完成信号，任务完结");
                }
                else
                {
                    var nextStepDef = steps.FirstOrDefault(s => s.StepIndex == nextStepIndex);
                    task.AdvanceStep(nextStepIndex, nextStepDef?.ActiveLeg ?? currentStep?.ActiveLeg);
                    return StepExecutionResult.Success(nextStepIndex, $"唤醒并推进至第 {nextStepIndex} 步");
                }
            }

            return StepExecutionResult.Success(task.StepIndex + 1);
        }

        /// <inheritdoc />
        public async Task<bool> RollbackToStepAsync(
            IWorkflowTask task,
            int targetStepIndex,
            string reason,
            string workflowKey = "Default",
            CancellationToken cancellationToken = default)
        {
            Check.NotNull(task, nameof(task));
            Check.NotNullOrWhiteSpace(reason, nameof(reason));

            if (!_workflows.TryGetValue(workflowKey, out var steps))
            {
                _logger.LogWarning("无法执行 SAGA 补偿回退: 未找到工作流 [{Key}]", workflowKey);
                return false;
            }

            // 获取从当前步骤到目标步骤之间的所有已执行步骤（倒序补偿）
            var stepsToCompensate = steps
                .Where(s => s.StepIndex <= task.StepIndex && s.StepIndex >= targetStepIndex)
                .OrderByDescending(s => s.StepIndex)
                .ToList();

            var context = new WorkflowStepContext(task, _serviceProvider, cancellationToken);

            foreach (var step in stepsToCompensate)
            {
                try
                {
                    _logger.LogInformation("正在对任务 [{TaskCode}] 步骤 [{StepIndex}:{StepName}] 执行 SAGA 逆向补偿...",
                        task.TaskCode, step.StepIndex, step.StepName);
                    await step.CompensateAsync(context);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "任务 [{TaskCode}] 步骤 [{StepIndex}:{StepName}] 补偿执行失败: {Message}",
                        task.TaskCode, step.StepIndex, step.StepName, ex.Message);
                    // 补偿失败仍记录并继续向上层报告
                }
            }

            // 领域实体步退回滚
            task.RollbackToStep(targetStepIndex, reason);
            _logger.LogInformation("任务 [{TaskCode}] 成功完成 SAGA 步退回滚至第 {Target} 步 (原因: {Reason})。",
                task.TaskCode, targetStepIndex, reason);

            return true;
        }
    }
}
