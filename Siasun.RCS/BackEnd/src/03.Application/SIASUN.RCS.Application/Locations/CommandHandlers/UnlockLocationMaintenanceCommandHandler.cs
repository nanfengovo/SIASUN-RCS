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
    /// 解除库位人工维护封锁命令处理器
    /// </summary>
    public class UnlockLocationMaintenanceCommandHandler : ICommandHandler<UnlockLocationMaintenanceCommand, LocationLockOperationResultDto>
    {
        private readonly ILocationLocker _locationLocker;

        public UnlockLocationMaintenanceCommandHandler(ILocationLocker locationLocker)
        {
            _locationLocker = locationLocker;
        }

        public async Task<LocationLockOperationResultDto> Handle(UnlockLocationMaintenanceCommand cmd, CancellationToken cancellationToken)
        {
            Check.NotNull(cmd, nameof(cmd));

            var success = await _locationLocker.UnlockMaintenanceAsync(cmd.LocationCode, cancellationToken);
            if (!success)
            {
                return LocationLockOperationResultDto.Fail(
                    cmd.LocationCode,
                    $"库位 [{cmd.LocationCode}] 未处于人工维护状态或已被解除",
                    beforeState: "None",
                    afterState: "None");
            }

            return LocationLockOperationResultDto.Ok(
                cmd.LocationCode,
                $"库位 [{cmd.LocationCode}] 维护封锁已成功解除，已恢复正常调度可用状态",
                beforeState: "Maintenance",
                afterState: "Unlocked");
        }
    }
}
