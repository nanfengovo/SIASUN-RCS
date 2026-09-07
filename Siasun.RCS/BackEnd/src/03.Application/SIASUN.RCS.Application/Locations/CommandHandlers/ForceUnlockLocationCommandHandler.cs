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
    /// 强制解除库位锁命令处理器
    /// 纯净领域业务逻辑，自动接入 CQRS 操作日志审计
    /// </summary>
    public class ForceUnlockLocationCommandHandler : ICommandHandler<ForceUnlockLocationCommand, LocationLockOperationResultDto>
    {
        private readonly ILocationLocker _locationLocker;

        public ForceUnlockLocationCommandHandler(ILocationLocker locationLocker)
        {
            _locationLocker = locationLocker;
        }

        public async Task<LocationLockOperationResultDto> Handle(ForceUnlockLocationCommand cmd, CancellationToken cancellationToken)
        {
            Check.NotNull(cmd, nameof(cmd));

            var affectedTaskId = await _locationLocker.ForceUnlockAsync(cmd.LocationCode, cmd.Reason, cancellationToken);
            return LocationLockOperationResultDto.Ok(
                cmd.LocationCode,
                $"已强制解除库位 [{cmd.LocationCode}] 的锁定状态",
                affectedTaskId,
                beforeState: "Locked",
                afterState: "Unlocked");
        }
    }
}
