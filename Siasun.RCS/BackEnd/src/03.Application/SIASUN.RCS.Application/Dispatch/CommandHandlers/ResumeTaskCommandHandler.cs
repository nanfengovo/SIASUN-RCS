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
    /// 调度员人工干预恢复失败任务命令处理器
    /// 严格遵循显式领域状态转换铁律，经 CQRS 管道自动生成前后状态审计快照
    /// </summary>
    public class ResumeTaskCommandHandler : ICommandHandler<ResumeTaskCommand, DispatchInterventionResultDto>
    {
        private readonly IRepository<AgvTask, Guid> _taskRepository;

        /// <summary>
        /// 构造函数注入任务仓储
        /// </summary>
        public ResumeTaskCommandHandler(IRepository<AgvTask, Guid> taskRepository)
        {
            _taskRepository = taskRepository;
        }

        /// <summary>
        /// 处理恢复失败任务命令
        /// </summary>
        public async Task<DispatchInterventionResultDto> Handle(ResumeTaskCommand cmd, CancellationToken cancellationToken)
        {
            Check.NotNull(cmd, nameof(cmd));
            if (string.IsNullOrWhiteSpace(cmd.TaskId))
            {
                throw new UserFriendlyException("任务编号不能为空");
            }
            if (string.IsNullOrWhiteSpace(cmd.Reason))
            {
                throw new UserFriendlyException("人工干预必须填写恢复原因以备事故追溯");
            }

            var taskEntity = await FindTaskEntityAsync(cmd.TaskId);
            if (taskEntity == null)
            {
                throw new EntityNotFoundException(typeof(AgvTask), cmd.TaskId);
            }

            var beforeState = $"{taskEntity.Status}[Step={taskEntity.StepIndex}]";

            // 显式领域方法驱动状态复原与步进重试
            taskEntity.ResumeFromFailure(cmd.Reason, cmd.RetryCurrentStep);

            await _taskRepository.UpdateAsync(taskEntity, autoSave: true, cancellationToken: cancellationToken);

            var afterState = $"{taskEntity.Status}[Step={taskEntity.StepIndex}]";

            return new DispatchInterventionResultDto
            {
                Success = true,
                Message = $"任务 [{taskEntity.TaskCode}] 已成功恢复为 Running 状态 (StepIndex={taskEntity.StepIndex})",
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
