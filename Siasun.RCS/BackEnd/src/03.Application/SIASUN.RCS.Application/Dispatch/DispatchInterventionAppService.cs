using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SIASUN.RCS.Dispatch.Dtos;
using SIASUN.RCS.Interfaces.OperationLogs;
using SIASUN.RCS.Logs.OperatorLog;
using SIASUN.RCS.Logs.OperatorLogs;
using SIASUN.RCS.Permissions;
using SIASUN.RCS.Tasks;
using SIASUN.RCS.Vehicles;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace SIASUN.RCS.Dispatch
{
    /// <summary>
    /// 调度员人工干预应用服务实现
    /// 承载现场调度员对生产现场异常任务与车辆的人工干预，全量无死角记录定分止争责任审计
    /// 严格遵守 L3 铁律：仓储强制依赖，真实实体状态流转，杜绝捏造审计状态
    /// </summary>
    [Authorize(RCSPermissions.DispatchIntervention.Default)]
    public class DispatchInterventionAppService : ApplicationService, IDispatchInterventionAppService
    {
        private readonly IOperationLogRecorder _operationLogRecorder;
        private readonly IRepository<AgvTask, Guid> _taskRepository;
        private readonly IRepository<AgvVehicle, Guid> _vehicleRepository;

        /// <summary>
        /// 构造函数注入操作日志记录器与核心聚合根仓储
        /// </summary>
        /// <param name="operationLogRecorder">操作审计记录器</param>
        /// <param name="taskRepository">调度任务仓储</param>
        /// <param name="vehicleRepository">车辆仓储</param>
        public DispatchInterventionAppService(
            IOperationLogRecorder operationLogRecorder,
            IRepository<AgvTask, Guid> taskRepository,
            IRepository<AgvVehicle, Guid> vehicleRepository)
        {
            _operationLogRecorder = operationLogRecorder;
            _taskRepository = taskRepository;
            _vehicleRepository = vehicleRepository;
        }

        /// <summary>
        /// 调度员人工干预取消指定的调度任务
        /// </summary>
        /// <param name="input">取消任务参数</param>
        /// <returns>干预操作结果</returns>
        [Authorize(RCSPermissions.DispatchIntervention.Cancel)]
        [OperationLog(Module = "Dispatch", Action = "CancelTask", TargetType = "Task", Description = "调度员人工干预取消任务")]
        public async Task<DispatchInterventionResultDto> CancelTaskAsync(CancelTaskInput input)
        {
            Check.NotNull(input, nameof(input));
            if (string.IsNullOrWhiteSpace(input.TaskId))
            {
                throw new UserFriendlyException("任务编号不能为空");
            }
            if (string.IsNullOrWhiteSpace(input.Reason))
            {
                throw new UserFriendlyException("人工干预必须填写取消原因以备事故追溯");
            }

            var taskEntity = await FindTaskEntityAsync(input.TaskId);
            if (taskEntity == null)
            {
                RecordInterventionLog(
                    action: "CancelTask",
                    targetType: "Task",
                    targetId: input.TaskId,
                    taskId: input.TaskId,
                    agvId: input.AgvId,
                    beforeState: null,
                    afterState: null,
                    reason: input.Reason,
                    description: $"调度员人工干预取消任务 [{input.TaskId}] 失败：目标任务实体不存在",
                    status: OperationLogStatus.Failed,
                    errorMessage: $"未找到任务 [{input.TaskId}]");

                throw new EntityNotFoundException(typeof(AgvTask), input.TaskId);
            }

            var beforeState = taskEntity.Status.ToString();
            string afterState;
            try
            {
                taskEntity.Cancel(input.Reason);
                await _taskRepository.UpdateAsync(taskEntity, autoSave: true);
                afterState = taskEntity.Status.ToString();
            }
            catch (Exception ex)
            {
                RecordInterventionLog(
                    action: "CancelTask",
                    targetType: "Task",
                    targetId: input.TaskId,
                    taskId: input.TaskId,
                    agvId: input.AgvId,
                    beforeState: beforeState,
                    afterState: beforeState,
                    reason: input.Reason,
                    description: $"调度员人工干预取消任务 [{input.TaskId}] 失败：{ex.Message}",
                    status: OperationLogStatus.Failed,
                    errorMessage: ex.Message);
                throw;
            }

            RecordInterventionLog(
                action: "CancelTask",
                targetType: "Task",
                targetId: input.TaskId,
                taskId: input.TaskId,
                agvId: input.AgvId,
                beforeState: beforeState,
                afterState: afterState,
                reason: input.Reason,
                description: $"调度员人工干预取消任务 [{input.TaskId}]，原因：{input.Reason}",
                status: OperationLogStatus.Success);

            return new DispatchInterventionResultDto
            {
                Success = true,
                Message = $"任务 [{input.TaskId}] 已成功取消",
                TargetType = "Task",
                TargetId = input.TaskId,
                CurrentState = afterState,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 调度员人工强制完结指定的调度任务
        /// </summary>
        /// <param name="input">强制完结参数</param>
        /// <returns>干预操作结果</returns>
        [Authorize(RCSPermissions.DispatchIntervention.ForceEnd)]
        [OperationLog(Module = "Dispatch", Action = "ForceEndTask", TargetType = "Task", Description = "调度员人工干预强制完结任务")]
        public async Task<DispatchInterventionResultDto> ForceEndTaskAsync(ForceEndTaskInput input)
        {
            Check.NotNull(input, nameof(input));
            if (string.IsNullOrWhiteSpace(input.TaskId))
            {
                throw new UserFriendlyException("任务编号不能为空");
            }
            if (string.IsNullOrWhiteSpace(input.Reason))
            {
                throw new UserFriendlyException("人工干预必须填写强制完结原因");
            }

            var taskEntity = await FindTaskEntityAsync(input.TaskId);
            if (taskEntity == null)
            {
                RecordInterventionLog(
                    action: "ForceEndTask",
                    targetType: "Task",
                    targetId: input.TaskId,
                    taskId: input.TaskId,
                    agvId: input.AgvId,
                    beforeState: null,
                    afterState: null,
                    reason: input.Reason,
                    description: $"调度员人工强制完结任务 [{input.TaskId}] 失败：目标任务实体不存在",
                    status: OperationLogStatus.Failed,
                    errorMessage: $"未找到任务 [{input.TaskId}]");

                throw new EntityNotFoundException(typeof(AgvTask), input.TaskId);
            }

            var beforeState = taskEntity.Status.ToString();
            string afterState;
            try
            {
                taskEntity.ForceEnd(input.Reason);
                await _taskRepository.UpdateAsync(taskEntity, autoSave: true);
                afterState = taskEntity.Status.ToString();
            }
            catch (Exception ex)
            {
                RecordInterventionLog(
                    action: "ForceEndTask",
                    targetType: "Task",
                    targetId: input.TaskId,
                    taskId: input.TaskId,
                    agvId: input.AgvId,
                    beforeState: beforeState,
                    afterState: beforeState,
                    reason: input.Reason,
                    description: $"调度员人工强制完结任务 [{input.TaskId}] 失败：{ex.Message}",
                    status: OperationLogStatus.Failed,
                    errorMessage: ex.Message);
                throw;
            }

            RecordInterventionLog(
                action: "ForceEndTask",
                targetType: "Task",
                targetId: input.TaskId,
                taskId: input.TaskId,
                agvId: input.AgvId,
                beforeState: beforeState,
                afterState: afterState,
                reason: input.Reason,
                description: $"调度员人工强制完结任务 [{input.TaskId}]，原因：{input.Reason}",
                status: OperationLogStatus.Success);

            return new DispatchInterventionResultDto
            {
                Success = true,
                Message = $"任务 [{input.TaskId}] 已强制完结为成功状态",
                TargetType = "Task",
                TargetId = input.TaskId,
                CurrentState = afterState,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 调度员人工指定特定 AGV 车辆执行任务
        /// </summary>
        /// <param name="input">指派车辆参数</param>
        /// <returns>干预操作结果</returns>
        [Authorize(RCSPermissions.DispatchIntervention.Assign)]
        [OperationLog(Module = "Dispatch", Action = "AssignVehicle", TargetType = "Task", Description = "调度员人工干预指派任务车辆")]
        public async Task<DispatchInterventionResultDto> AssignVehicleAsync(AssignVehicleInput input)
        {
            Check.NotNull(input, nameof(input));
            if (string.IsNullOrWhiteSpace(input.TaskId))
            {
                throw new UserFriendlyException("任务编号不能为空");
            }
            if (string.IsNullOrWhiteSpace(input.AgvId))
            {
                throw new UserFriendlyException("目标车辆编号不能为空");
            }
            if (string.IsNullOrWhiteSpace(input.Reason))
            {
                throw new UserFriendlyException("人工指派车辆必须填写操作原因");
            }

            var taskEntity = await FindTaskEntityAsync(input.TaskId);
            if (taskEntity == null)
            {
                RecordInterventionLog(
                    action: "AssignVehicle",
                    targetType: "Task",
                    targetId: input.TaskId,
                    taskId: input.TaskId,
                    agvId: input.AgvId,
                    beforeState: null,
                    afterState: null,
                    reason: input.Reason,
                    description: $"调度员指定车辆 [{input.AgvId}] 执行任务 [{input.TaskId}] 失败：目标任务不存在",
                    status: OperationLogStatus.Failed,
                    errorMessage: $"未找到任务 [{input.TaskId}]");

                throw new EntityNotFoundException(typeof(AgvTask), input.TaskId);
            }

            var vehicleEntity = await FindVehicleEntityAsync(input.AgvId);
            if (vehicleEntity == null)
            {
                var currentTaskVehicle = taskEntity.AssignedVehicleCode ?? "Unassigned";
                RecordInterventionLog(
                    action: "AssignVehicle",
                    targetType: "Task",
                    targetId: input.TaskId,
                    taskId: input.TaskId,
                    agvId: input.AgvId,
                    beforeState: currentTaskVehicle,
                    afterState: currentTaskVehicle,
                    reason: input.Reason,
                    description: $"调度员指定车辆 [{input.AgvId}] 执行任务 [{input.TaskId}] 失败：目标车辆不存在",
                    status: OperationLogStatus.Failed,
                    errorMessage: $"未找到车辆 [{input.AgvId}]");

                throw new EntityNotFoundException(typeof(AgvVehicle), input.AgvId);
            }

            var beforeState = taskEntity.AssignedVehicleCode ?? "Unassigned";
            string afterState;
            try
            {
                taskEntity.AssignVehicle(vehicleEntity.Id, vehicleEntity.VehicleCode, input.Reason);
                await _taskRepository.UpdateAsync(taskEntity, autoSave: true);

                vehicleEntity.AssignTask(taskEntity.Id, taskEntity.TaskCode);
                await _vehicleRepository.UpdateAsync(vehicleEntity, autoSave: true);

                afterState = $"Assigned:{vehicleEntity.VehicleCode}";
            }
            catch (Exception ex)
            {
                RecordInterventionLog(
                    action: "AssignVehicle",
                    targetType: "Task",
                    targetId: input.TaskId,
                    taskId: input.TaskId,
                    agvId: input.AgvId,
                    beforeState: beforeState,
                    afterState: beforeState,
                    reason: input.Reason,
                    description: $"调度员指定车辆 [{input.AgvId}] 执行任务 [{input.TaskId}] 失败：{ex.Message}",
                    status: OperationLogStatus.Failed,
                    errorMessage: ex.Message);
                throw;
            }

            RecordInterventionLog(
                action: "AssignVehicle",
                targetType: "Task",
                targetId: input.TaskId,
                taskId: input.TaskId,
                agvId: input.AgvId,
                beforeState: beforeState,
                afterState: afterState,
                reason: input.Reason,
                description: $"调度员指定车辆 [{input.AgvId}] 执行任务 [{input.TaskId}]，原因：{input.Reason}",
                status: OperationLogStatus.Success);

            return new DispatchInterventionResultDto
            {
                Success = true,
                Message = $"已人工指定车辆 [{input.AgvId}] 执行任务 [{input.TaskId}]",
                TargetType = "Task",
                TargetId = input.TaskId,
                CurrentState = afterState,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 调度员人工复位处于异常或告警状态的 AGV 车辆
        /// </summary>
        /// <param name="input">复位车辆参数</param>
        /// <returns>干预操作结果</returns>
        [Authorize(RCSPermissions.DispatchIntervention.Reset)]
        [OperationLog(Module = "Dispatch", Action = "ResetVehicle", TargetType = "Vehicle", Description = "调度员人工干预复位车辆报警")]
        public async Task<DispatchInterventionResultDto> ResetVehicleAsync(ResetVehicleInput input)
        {
            Check.NotNull(input, nameof(input));
            if (string.IsNullOrWhiteSpace(input.AgvId))
            {
                throw new UserFriendlyException("车辆编号不能为空");
            }
            if (string.IsNullOrWhiteSpace(input.Reason))
            {
                throw new UserFriendlyException("车辆复位必须填写处置依据与原因");
            }

            var vehicleEntity = await FindVehicleEntityAsync(input.AgvId);
            if (vehicleEntity == null)
            {
                RecordInterventionLog(
                    action: "ResetVehicle",
                    targetType: "Vehicle",
                    targetId: input.AgvId,
                    taskId: null,
                    agvId: input.AgvId,
                    beforeState: null,
                    afterState: null,
                    reason: input.Reason,
                    description: $"调度员人工复位车辆 [{input.AgvId}] 失败：目标车辆不存在",
                    status: OperationLogStatus.Failed,
                    errorMessage: $"未找到车辆 [{input.AgvId}]");

                throw new EntityNotFoundException(typeof(AgvVehicle), input.AgvId);
            }

            var beforeState = vehicleEntity.Status.ToString();
            string afterState;
            try
            {
                vehicleEntity.Reset(input.Reason);
                await _vehicleRepository.UpdateAsync(vehicleEntity, autoSave: true);
                afterState = vehicleEntity.Status.ToString();
            }
            catch (Exception ex)
            {
                RecordInterventionLog(
                    action: "ResetVehicle",
                    targetType: "Vehicle",
                    targetId: input.AgvId,
                    taskId: null,
                    agvId: input.AgvId,
                    beforeState: beforeState,
                    afterState: beforeState,
                    reason: input.Reason,
                    description: $"调度员人工复位车辆 [{input.AgvId}] 失败：{ex.Message}",
                    status: OperationLogStatus.Failed,
                    errorMessage: ex.Message);
                throw;
            }

            RecordInterventionLog(
                action: "ResetVehicle",
                targetType: "Vehicle",
                targetId: input.AgvId,
                taskId: null,
                agvId: input.AgvId,
                beforeState: beforeState,
                afterState: afterState,
                reason: input.Reason,
                description: $"调度员人工复位车辆 [{input.AgvId}]，原因：{input.Reason}",
                status: OperationLogStatus.Success);

            return new DispatchInterventionResultDto
            {
                Success = true,
                Message = $"车辆 [{input.AgvId}] 已人工复位为 Idle 状态",
                TargetType = "Vehicle",
                TargetId = input.AgvId,
                CurrentState = afterState,
                Timestamp = DateTime.UtcNow
            };
        }

        private void RecordInterventionLog(
            string action,
            string targetType,
            string targetId,
            string? taskId,
            string? agvId,
            string? beforeState,
            string? afterState,
            string? reason,
            string description,
            OperationLogStatus status,
            string? errorMessage = null)
        {
            _operationLogRecorder.Record(new OperationLogContext
            {
                Module = "Dispatch",
                Action = action,
                TargetType = targetType,
                TargetId = targetId,
                TaskId = taskId,
                AgvId = agvId,
                BeforeState = beforeState,
                AfterState = afterState,
                Reason = reason,
                Description = description
            }, status, errorMessage);
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
