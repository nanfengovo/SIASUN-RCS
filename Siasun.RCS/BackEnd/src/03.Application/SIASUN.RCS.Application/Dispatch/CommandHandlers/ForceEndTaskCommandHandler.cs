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
    /// 调度员人工强制完结任务命令处理器
    /// 纯净领域业务逻辑，零手写日志代码，全生命周期由 CQRS 管道自动审计
    /// </summary>
    public class ForceEndTaskCommandHandler : ICommandHandler<ForceEndTaskCommand, DispatchInterventionResultDto>
    {
        private readonly IRepository<AgvTask, Guid> _taskRepository;

        /// <summary>
        /// 构造函数注入任务仓储
        /// </summary>
        /// <param name="taskRepository">任务仓储</param>
        public ForceEndTaskCommandHandler(IRepository<AgvTask, Guid> taskRepository)
        {
            _taskRepository = taskRepository;
        }

        /// <summary>
        /// 执行强制完结任务命令
        /// </summary>
        public async Task<DispatchInterventionResultDto> Handle(ForceEndTaskCommand cmd, CancellationToken cancellationToken)
        {
            Check.NotNull(cmd, nameof(cmd));
            if (string.IsNullOrWhiteSpace(cmd.TaskId))
            {
                throw new UserFriendlyException("任务编号不能为空");
            }
            if (string.IsNullOrWhiteSpace(cmd.Reason))
            {
                throw new UserFriendlyException("人工干预必须填写强制完结原因");
            }

            var taskEntity = await FindTaskEntityAsync(cmd.TaskId);
            if (taskEntity == null)
            {
                throw new EntityNotFoundException(typeof(AgvTask), cmd.TaskId);
            }

            var beforeState = taskEntity.Status.ToString();
            taskEntity.ForceEnd(cmd.Reason);
            await _taskRepository.UpdateAsync(taskEntity, autoSave: true, cancellationToken: cancellationToken);
            var afterState = taskEntity.Status.ToString();

            return new DispatchInterventionResultDto
            {
                Success = true,
                Message = $"任务 [{cmd.TaskId}] 已强制完结为成功状态",
                TargetType = "Task",
                TargetId = cmd.TaskId,
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

