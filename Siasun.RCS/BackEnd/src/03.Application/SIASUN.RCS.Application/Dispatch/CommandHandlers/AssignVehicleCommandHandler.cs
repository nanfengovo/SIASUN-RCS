using System;
using System.Threading;
using System.Threading.Tasks;
using SIASUN.RCS.Commands;
using SIASUN.RCS.Dispatch.Commands;
using SIASUN.RCS.Dispatch.Dtos;
using SIASUN.RCS.Tasks;
using SIASUN.RCS.Vehicles;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace SIASUN.RCS.Dispatch.CommandHandlers
{
    /// <summary>
    /// 调度员人工指派车辆执行任务命令处理器
    /// 纯净领域业务逻辑，零手写日志代码，全生命周期由 CQRS 管道自动审计
    /// </summary>
    public class AssignVehicleCommandHandler : ICommandHandler<AssignVehicleCommand, DispatchInterventionResultDto>
    {
        private readonly IRepository<AgvTask, Guid> _taskRepository;
        private readonly IRepository<AgvVehicle, Guid> _vehicleRepository;

        /// <summary>
        /// 构造函数注入任务与车辆仓储
        /// </summary>
        /// <param name="taskRepository">任务仓储</param>
        /// <param name="vehicleRepository">车辆仓储</param>
        public AssignVehicleCommandHandler(
            IRepository<AgvTask, Guid> taskRepository,
            IRepository<AgvVehicle, Guid> vehicleRepository)
        {
            _taskRepository = taskRepository;
            _vehicleRepository = vehicleRepository;
        }

        /// <summary>
        /// 执行指派车辆命令
        /// </summary>
        public async Task<DispatchInterventionResultDto> Handle(AssignVehicleCommand cmd, CancellationToken cancellationToken)
        {
            Check.NotNull(cmd, nameof(cmd));
            if (string.IsNullOrWhiteSpace(cmd.TaskId))
            {
                throw new UserFriendlyException("任务编号不能为空");
            }
            if (string.IsNullOrWhiteSpace(cmd.AgvId))
            {
                throw new UserFriendlyException("目标车辆编号不能为空");
            }
            if (string.IsNullOrWhiteSpace(cmd.Reason))
            {
                throw new UserFriendlyException("人工指派车辆必须填写操作原因");
            }

            var taskEntity = await FindTaskEntityAsync(cmd.TaskId);
            if (taskEntity == null)
            {
                throw new EntityNotFoundException(typeof(AgvTask), cmd.TaskId);
            }

            var vehicleEntity = await FindVehicleEntityAsync(cmd.AgvId);
            if (vehicleEntity == null)
            {
                throw new EntityNotFoundException(typeof(AgvVehicle), cmd.AgvId);
            }

            var beforeState = taskEntity.AssignedVehicleCode ?? "Unassigned";
            taskEntity.AssignVehicle(vehicleEntity.Id, vehicleEntity.VehicleCode, cmd.Reason);
            await _taskRepository.UpdateAsync(taskEntity, autoSave: true, cancellationToken: cancellationToken);

            vehicleEntity.AssignTask(taskEntity.Id, taskEntity.TaskCode);
            await _vehicleRepository.UpdateAsync(vehicleEntity, autoSave: true, cancellationToken: cancellationToken);

            var afterState = $"Assigned:{vehicleEntity.VehicleCode}";

            return new DispatchInterventionResultDto
            {
                Success = true,
                Message = $"已人工指定车辆 [{cmd.AgvId}] 执行任务 [{cmd.TaskId}]",
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

        private async Task<AgvVehicle?> FindVehicleEntityAsync(string vehicleIdOrCode)
        {
            if (Guid.TryParse(vehicleIdOrCode, out var guid))
            {
                var byId = await _vehicleRepository.FindAsync(guid);
                if (byId != null) return byId;
            }

            return await _vehicleRepository.FindAsync(x => x.VehicleCode == vehicleIdOrCode);
        }
    }
}

