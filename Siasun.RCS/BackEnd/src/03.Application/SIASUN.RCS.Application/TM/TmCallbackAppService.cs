using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Tasks;
using SIASUN.RCS.Tasks.Workflow;
using SIASUN.RCS.TM.Dtos;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace SIASUN.RCS.TM
{
    /// <summary>
    /// TM 报文状态回调应用服务实现（精准反查，杜绝字符串切分 hack，驱动工作流引擎步进）
    /// </summary>
    public class TmCallbackAppService : ApplicationService, ITmCallbackAppService
    {
        private readonly ITaskSerialRegistry _serialRegistry;
        private readonly IRepository<AgvTask, Guid> _taskRepository;
        private readonly ITaskWorkflowEngine _workflowEngine;
        private readonly ILogger<TmCallbackAppService> _logger;

        /// <summary>
        /// 构造函数注入序列号注册表、任务仓储、工作流引擎与日志组件
        /// </summary>
        public TmCallbackAppService(
            ITaskSerialRegistry serialRegistry,
            IRepository<AgvTask, Guid> taskRepository,
            ITaskWorkflowEngine workflowEngine,
            ILogger<TmCallbackAppService> logger)
        {
            _serialRegistry = serialRegistry;
            _taskRepository = taskRepository;
            _workflowEngine = workflowEngine;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<TmCallbackResultDto> HandleCallbackAsync(TmCallbackInput input)
        {
            Check.NotNull(input, nameof(input));
            Check.NotNullOrWhiteSpace(input.TmSerial, nameof(input.TmSerial));

            // 1. 通过统一注册表精确定位内部任务（零 hack 字符串解析）
            var mapping = await _serialRegistry.FindByTmSerialAsync(input.TmSerial);
            if (mapping == null)
            {
                _logger.LogWarning("收到未知或已过期的 TM 回调流水号: [TmSerial={TmSerial}, Status={Status}]", input.TmSerial, input.Status);
                return new TmCallbackResultDto
                {
                    Success = false,
                    Message = $"未找到 TM 序列号 [{input.TmSerial}] 关联的内部任务映射，可能已过期或结单清理"
                };
            }

            var task = await _taskRepository.FindAsync(mapping.TaskId);
            if (task == null)
            {
                _logger.LogError("TM 回调匹配到的内部任务不存在: [TaskId={TaskId}, TmSerial={TmSerial}]", mapping.TaskId, input.TmSerial);
                return new TmCallbackResultDto
                {
                    Success = false,
                    Message = $"任务实体 [{mapping.TaskId}] 不存在"
                };
            }

            _logger.LogInformation("收到有效 TM 回调: [TaskCode={TaskCode}, Leg={Leg}, Step={StepIndex}, Status={Status}]",
                task.TaskCode, mapping.Leg, mapping.StepIndex, input.Status);

            // 2. 根据 TM 回调状态驱动领域生命周期或工作流推进
            if (string.Equals(input.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(mapping.WaitingEvent))
                {
                    // 唤醒处于挂起状态的工作流步骤
                    await _workflowEngine.ResumeBySignalAsync(
                        task,
                        mapping.WaitingEvent,
                        payload: input.Payload,
                        workflowKey: task.WorkflowDefinitionId ?? "Default");
                }
                else if (task.Status == AgvTaskStatus.Running)
                {
                    // 默认步进推进
                    task.AdvanceStep(task.StepIndex + 1);
                }

                await _taskRepository.UpdateAsync(task, autoSave: true);

                // 若任务已彻底成功或终结，清理序列号映射
                if (task.Status == AgvTaskStatus.Succeeded)
                {
                    await _serialRegistry.RemoveByTaskIdAsync(task.Id);
                }

                return new TmCallbackResultDto
                {
                    Success = true,
                    TaskCode = task.TaskCode,
                    Leg = mapping.Leg,
                    CurrentTaskStatus = task.Status.ToString(),
                    CurrentStepIndex = task.StepIndex,
                    Message = $"TM 航段 [{mapping.Leg}] 已成功完成，任务步进已推进至第 {task.StepIndex} 步"
                };
            }
            else if (string.Equals(input.Status, "Failed", StringComparison.OrdinalIgnoreCase))
            {
                var error = input.ErrorMessage ?? input.ErrorCode ?? "底层动作执行异常";
                task.Fail($"TM 航段 [{mapping.Leg}] 报告执行失败: {error}");
                await _taskRepository.UpdateAsync(task, autoSave: true);

                return new TmCallbackResultDto
                {
                    Success = true,
                    TaskCode = task.TaskCode,
                    Leg = mapping.Leg,
                    CurrentTaskStatus = task.Status.ToString(),
                    CurrentStepIndex = task.StepIndex,
                    Message = $"TM 航段 [{mapping.Leg}] 失败，任务已转入 Failed 状态"
                };
            }

            // 中间状态（如 Executing），仅记录日志
            return new TmCallbackResultDto
            {
                Success = true,
                TaskCode = task.TaskCode,
                Leg = mapping.Leg,
                CurrentTaskStatus = task.Status.ToString(),
                CurrentStepIndex = task.StepIndex,
                Message = $"TM 航段 [{mapping.Leg}] 处于中间推进状态: {input.Status}"
            };
        }
    }
}
