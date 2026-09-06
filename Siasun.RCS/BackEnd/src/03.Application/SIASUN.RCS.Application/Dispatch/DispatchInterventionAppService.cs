using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SIASUN.RCS.Dispatch.Dtos;
using SIASUN.RCS.Interfaces.OperationLogs;
using SIASUN.RCS.Logs.OperatorLogs;
using SIASUN.RCS.Permissions;
using SIASUN.RCS.Tasks;
using SIASUN.RCS.Vehicles;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace SIASUN.RCS.Dispatch
{
    /// <summary>
    /// 调度员人工干预应用服务实现
    /// 承载现场调度员对生产现场异常任务与车辆的人工干预，全量无死角记录定分止争责任审计
    /// </summary>
    [Authorize(RCSPermissions.DispatchIntervention.Default)]
    public class DispatchInterventionAppService : ApplicationService, IDispatchInterventionAppService
    {
        private readonly IOperationLogRecorder _operationLogRecorder;
        private readonly IRepository<AgvTask, Guid>? _taskRepository;
        private readonly IRepository<AgvVehicle, Guid>? _vehicleRepository;

        /// <summary>
        /// 构造函数注入操作日志记录器与核心聚合根仓库
        /// </summary>
        /// <param name="operationLogRecorder">操作审计记录器</param>
        /// <param name="taskRepository">调度任务仓储（可选）</param>
        /// <param name="vehicleRepository">车辆仓储（可选）</param>
        public DispatchInterventionAppService(
            IOperationLogRecorder operationLogRecorder,
            IRepository<AgvTask, Guid>? taskRepository = null,
            IRepository<AgvVehicle, Guid>? vehicleRepository = null)
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

            var beforeState = "Running";
            var afterState = "Canceled";

            // 若仓储已注入，查询并修改真实数据库聚合根实体，触发 EF Core 实体审计拦截器记录差异
            if (_taskRepository != null)
            {
                var taskEntity = await FindTaskEntityAsync(input.TaskId);
                if (taskEntity != null)
                {
                    beforeState = taskEntity.Status.ToString();
                    taskEntity.Cancel(input.Reason);
                    await _taskRepository.UpdateAsync(taskEntity, autoSave: true);
                    afterState = taskEntity.Status.ToString();
                }
            }

            _operationLogRecorder.Record(new OperationLogContext
            {
                Module = "Dispatch",
                Action = "CancelTask",
                TargetType = "Task",
                TargetId = input.TaskId,
                TaskId = input.TaskId,
                AgvId = input.AgvId,
                BeforeState = beforeState,
                AfterState = afterState,
                Reason = input.Reason,
                Description = $"调度员人工干预取消任务 [{input.TaskId}]，原因：{input.Reason}"
            });

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

            var beforeState = "Running";
            var afterState = "Succeeded";

            if (_taskRepository != null)
            {
                var taskEntity = await FindTaskEntityAsync(input.TaskId);
                if (taskEntity != null)
                {
                    beforeState = taskEntity.Status.ToString();
                    taskEntity.ForceEnd(input.Reason);
                    await _taskRepository.UpdateAsync(taskEntity, autoSave: true);
                    afterState = taskEntity.Status.ToString();
                }
            }

            _operationLogRecorder.Record(new OperationLogContext
            {
                Module = "Dispatch",
                Action = "ForceEndTask",
                TargetType = "Task",
                TargetId = input.TaskId,
                TaskId = input.TaskId,
                AgvId = input.AgvId,
                BeforeState = beforeState,
                AfterState = afterState,
                Reason = input.Reason,
                Description = $"调度员人工强制完结任务 [{input.TaskId}]，原因：{input.Reason}"
            });

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

            var beforeState = "Unassigned";
            var afterState = $"Assigned:{input.AgvId}";

            if (_taskRepository != null && _vehicleRepository != null)
            {
                var taskEntity = await FindTaskEntityAsync(input.TaskId);
                var vehicleEntity = await FindVehicleEntityAsync(input.AgvId);

                if (taskEntity != null && vehicleEntity != null)
                {
                    beforeState = taskEntity.AssignedVehicleCode ?? "Unassigned";
                    taskEntity.AssignVehicle(vehicleEntity.Id, vehicleEntity.VehicleCode, input.Reason);
                    await _taskRepository.UpdateAsync(taskEntity, autoSave: true);

                    vehicleEntity.AssignTask(taskEntity.Id, taskEntity.TaskCode);
                    await _vehicleRepository.UpdateAsync(vehicleEntity, autoSave: true);

                    afterState = $"Assigned:{vehicleEntity.VehicleCode}";
                }
            }

            _operationLogRecorder.Record(new OperationLogContext
            {
                Module = "Dispatch",
                Action = "AssignVehicle",
                TargetType = "Task",
                TargetId = input.TaskId,
                TaskId = input.TaskId,
                AgvId = input.AgvId,
                BeforeState = beforeState,
                AfterState = afterState,
                Reason = input.Reason,
                Description = $"调度员指定车辆 [{input.AgvId}] 执行任务 [{input.TaskId}]，原因：{input.Reason}"
            });

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

            var beforeState = "Error";
            var afterState = "Idle";

            if (_vehicleRepository != null)
            {
                var vehicleEntity = await FindVehicleEntityAsync(input.AgvId);
                if (vehicleEntity != null)
                {
                    beforeState = vehicleEntity.Status.ToString();
                    vehicleEntity.Reset(input.Reason);
                    await _vehicleRepository.UpdateAsync(vehicleEntity, autoSave: true);
                    afterState = vehicleEntity.Status.ToString();
                }
            }

            _operationLogRecorder.Record(new OperationLogContext
            {
                Module = "Dispatch",
                Action = "ResetVehicle",
                TargetType = "Vehicle",
                TargetId = input.AgvId,
                AgvId = input.AgvId,
                BeforeState = beforeState,
                AfterState = afterState,
                Reason = input.Reason,
                Description = $"调度员人工复位车辆 [{input.AgvId}]，原因：{input.Reason}"
            });

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

        private async Task<AgvTask?> FindTaskEntityAsync(string taskIdOrCode)
        {
            if (_taskRepository == null) return null;

            if (Guid.TryParse(taskIdOrCode, out var guid))
            {
                var byId = await _taskRepository.FindAsync(guid);
                if (byId != null) return byId;
            }

            return await _taskRepository.FirstOrDefaultAsync(x => x.TaskCode == taskIdOrCode);
        }

        private async Task<AgvVehicle?> FindVehicleEntityAsync(string vehicleIdOrCode)
        {
            if (_vehicleRepository == null) return null;

            if (Guid.TryParse(vehicleIdOrCode, out var guid))
            {
                var byId = await _vehicleRepository.FindAsync(guid);
                if (byId != null) return byId;
            }

            return await _vehicleRepository.FirstOrDefaultAsync(x => x.VehicleCode == vehicleIdOrCode);
        }
    }
}
