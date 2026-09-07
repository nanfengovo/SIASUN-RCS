using System;
using System.Threading;
using System.Threading.Tasks;
using SIASUN.RCS.Commands;
using SIASUN.RCS.Locations.Commands;
using SIASUN.RCS.Locations.Dtos;
using Volo.Abp;

namespace SIASUN.RCS.Locations.CommandHandlers
{
    /// <summary>
    /// 库位人工维护封锁命令处理器
    /// </summary>
    public class LockLocationForMaintenanceCommandHandler : ICommandHandler<LockLocationForMaintenanceCommand, LocationLockOperationResultDto>
    {
        private readonly ILocationLocker _locationLocker;

        public LockLocationForMaintenanceCommandHandler(ILocationLocker locationLocker)
        {
            _locationLocker = locationLocker;
        }

        public async Task<LocationLockOperationResultDto> Handle(LockLocationForMaintenanceCommand cmd, CancellationToken cancellationToken)
        {
            Check.NotNull(cmd, nameof(cmd));

            var success = await _locationLocker.LockForMaintenanceAsync(cmd.LocationCode, cmd.Reason, cancellationToken);
            if (!success)
            {
                return LocationLockOperationResultDto.Fail(
                    cmd.LocationCode,
                    $"库位 [{cmd.LocationCode}] 当前已被其他活动作业锁定，无法执行维护封锁",
                    beforeState: "Locked",
                    afterState: "Locked");
            }

            return LocationLockOperationResultDto.Ok(
                cmd.LocationCode,
                $"库位 [{cmd.LocationCode}] 已成功设置为人工维护封锁状态",
                beforeState: "Unlocked",
                afterState: "Maintenance");
        }
    }
}
