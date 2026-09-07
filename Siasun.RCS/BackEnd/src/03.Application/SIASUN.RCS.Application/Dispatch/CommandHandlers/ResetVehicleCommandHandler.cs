using System;
using System.Threading;
using System.Threading.Tasks;
using SIASUN.RCS.Commands;
using SIASUN.RCS.Dispatch.Commands;
using SIASUN.RCS.Dispatch.Dtos;
using SIASUN.RCS.Vehicles;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace SIASUN.RCS.Dispatch.CommandHandlers
{
    /// <summary>
    /// 调度员人工复位车辆命令处理器
    /// 纯净领域业务逻辑，零手写日志代码，全生命周期由 CQRS 管道自动审计
    /// </summary>
    public class ResetVehicleCommandHandler : ICommandHandler<ResetVehicleCommand, DispatchInterventionResultDto>
    {
        private readonly IRepository<AgvVehicle, Guid> _vehicleRepository;

        /// <summary>
        /// 构造函数注入车辆仓储
        /// </summary>
        /// <param name="vehicleRepository">车辆仓储</param>
        public ResetVehicleCommandHandler(IRepository<AgvVehicle, Guid> vehicleRepository)
        {
            _vehicleRepository = vehicleRepository;
        }

        /// <summary>
        /// 执行复位车辆命令
        /// </summary>
        public async Task<DispatchInterventionResultDto> Handle(ResetVehicleCommand cmd, CancellationToken cancellationToken)
        {
            Check.NotNull(cmd, nameof(cmd));
            if (string.IsNullOrWhiteSpace(cmd.AgvId))
            {
                throw new UserFriendlyException("车辆编号不能为空");
            }
            if (string.IsNullOrWhiteSpace(cmd.Reason))
            {
                throw new UserFriendlyException("车辆复位必须填写处置依据与原因");
            }

            var vehicleEntity = await FindVehicleEntityAsync(cmd.AgvId);
            if (vehicleEntity == null)
            {
                throw new EntityNotFoundException(typeof(AgvVehicle), cmd.AgvId);
            }

            var beforeState = vehicleEntity.Status.ToString();
            vehicleEntity.Reset(cmd.Reason);
            await _vehicleRepository.UpdateAsync(vehicleEntity, autoSave: true, cancellationToken: cancellationToken);
            var afterState = vehicleEntity.Status.ToString();

            return new DispatchInterventionResultDto
            {
                Success = true,
                Message = $"车辆 [{cmd.AgvId}] 已人工复位为 Idle 状态",
                TargetType = "Vehicle",
                TargetId = cmd.AgvId,
                BeforeState = beforeState,
                CurrentState = afterState,
                Timestamp = DateTime.UtcNow
            };
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

