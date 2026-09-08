using System;
using System.Threading;
using System.Threading.Tasks;
using SIASUN.RCS.Commands;
using SIASUN.RCS.Dispatch.Commands;
using SIASUN.RCS.Dispatch.Dtos;
using SIASUN.RCS.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace SIASUN.RCS.Dispatch.CommandHandlers
{
    /// <summary>
    /// 调度员人工干预回滚并重试任务命令处理器（SAGA 补偿与安全回退）
    /// 严格遵循显式领域状态转换铁律，经 CQRS 管道自动生成前后状态审计快照
    /// </summary>
    public class RollbackAndRetryCommandHandler : ICommandHandler<RollbackAndRetryCommand, DispatchInterventionResultDto>
    {
        private readonly IRepository<AgvTask, Guid> _taskRepository;

        /// <summary>
        /// 构造函数注入任务仓储
        /// </summary>
        public RollbackAndRetryCommandHandler(IRepository<AgvTask, Guid> taskRepository)
        {
            _taskRepository = taskRepository;
        }

        /// <summary>
        /// 处理回滚并重试命令
        /// </summary>
        public async Task<DispatchInterventionResultDto> Handle(RollbackAndRetryCommand cmd, CancellationToken cancellationToken)
        {
            Check.NotNull(cmd, nameof(cmd));
            if (string.IsNullOrWhiteSpace(cmd.TaskId))
            {
                throw new UserFriendlyException("任务编号不能为空");
            }
            if (string.IsNullOrWhiteSpace(cmd.Reason))
            {
                throw new UserFriendlyException("人工干预必须填写回滚原因以备事故追溯");
            }
            if (cmd.TargetStepIndex < 0)
            {
                throw new UserFriendlyException("目标回滚步骤索引不能为负数");
            }

            var taskEntity = await FindTaskEntityAsync(cmd.TaskId);
            if (taskEntity == null)
            {
                throw new EntityNotFoundException(typeof(AgvTask), cmd.TaskId);
            }

            var beforeState = $"{taskEntity.Status}[Step={taskEntity.StepIndex}]";

            // 如果当前处于 Failed 状态，先显式恢复生命周期到 Running
            if (taskEntity.Status == AgvTaskStatus.Failed)
            {
                taskEntity.ResumeFromFailure($"因回滚至第 {cmd.TargetStepIndex} 步而由调度员恢复: {cmd.Reason}", retryCurrentStep: true);
            }

            // 执行领域回滚与 SAGA 补偿事件抛出
            taskEntity.RollbackToStep(cmd.TargetStepIndex, cmd.Reason);

            await _taskRepository.UpdateAsync(taskEntity, autoSave: true, cancellationToken: cancellationToken);

            var afterState = $"{taskEntity.Status}[Step={taskEntity.StepIndex}]";

            return new DispatchInterventionResultDto
            {
                Success = true,
                Message = $"任务 [{taskEntity.TaskCode}] 已成功回滚至第 {cmd.TargetStepIndex} 步重试",
                TargetType = "Task",
                TargetId = taskEntity.TaskCode,
                BeforeState = beforeState,
                CurrentState = afterState,
                Timestamp = DateTime.UtcNow
            };
        }

        private async Task<AgvTask?> FindTaskEntityAsync(string taskIdOrCode)
        {
            if (Guid.TryParse(taskIdOrCode, out var guid))
            {
                var byId = await _taskRepository.FindAsync(guid);
                if (byId != null) return byId;
            }

            return await _taskRepository.FindAsync(x => x.TaskCode == taskIdOrCode);
        }
    }
}
