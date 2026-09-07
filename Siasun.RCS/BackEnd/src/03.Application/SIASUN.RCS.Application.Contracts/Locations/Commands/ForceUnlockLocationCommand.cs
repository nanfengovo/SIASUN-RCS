using System;
using SIASUN.RCS.Commands;
using SIASUN.RCS.Locations.Dtos;

namespace SIASUN.RCS.Locations.Commands
{
    /// <summary>
    /// 调度员强制解除库位锁命令
    /// 无论当前库位处于作业锁定还是维护锁定，均强制予以释放并记录责任审计
    /// </summary>
    public class ForceUnlockLocationCommand : ICommand<LocationLockOperationResultDto>, IAuditableCommand
    {
        /// <summary>
        /// 目标库位编码
        /// </summary>
        public string LocationCode { get; }

        /// <summary>
        /// 强制解锁原因（必填项，用于责任审计）
        /// </summary>
        public string Reason { get; }

        public string Module => "Location";
        public string Action => "ForceUnlock";
        public string TargetType => "Location";
        public string? TargetId => LocationCode;
        public string? Description => $"调度员强制解除库位 [{LocationCode}] 的锁";

        public ForceUnlockLocationCommand(string locationCode, string reason)
        {
            LocationCode = string.IsNullOrWhiteSpace(locationCode)
                ? throw new ArgumentNullException(nameof(locationCode))
                : locationCode.Trim();
            Reason = string.IsNullOrWhiteSpace(reason)
                ? throw new ArgumentNullException(nameof(reason))
                : reason.Trim();
        }
    }
}
