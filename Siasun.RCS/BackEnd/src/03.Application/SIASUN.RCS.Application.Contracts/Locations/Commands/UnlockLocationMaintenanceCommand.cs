using System;
using SIASUN.RCS.Commands;
using SIASUN.RCS.Locations.Dtos;

namespace SIASUN.RCS.Locations.Commands
{
    /// <summary>
    /// 调度员解除维护封锁命令
    /// 机台恢复或保养完成后恢复库位可用状态
    /// </summary>
    public class UnlockLocationMaintenanceCommand : ICommand<LocationLockOperationResultDto>, IAuditableCommand
    {
        /// <summary>
        /// 目标库位编码
        /// </summary>
        public string LocationCode { get; }

        /// <summary>
        /// 解封原因说明（必填项）
        /// </summary>
        public string Reason { get; }

        public string Module => "Location";
        public string Action => "UnlockMaintenance";
        public string TargetType => "Location";
        public string? TargetId => LocationCode;
        public string? Description => $"调度员人工解除库位 [{LocationCode}] 维护封锁";

        public UnlockLocationMaintenanceCommand(string locationCode, string reason)
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
