using System;
using SIASUN.RCS.Commands;
using SIASUN.RCS.Locations.Dtos;

namespace SIASUN.RCS.Locations.Commands
{
    /// <summary>
    /// 调度员人工维护封锁库位命令
    /// 适用于现场机台报修、传感器故障或地面清洁等场景，封锁后严禁派车前往
    /// </summary>
    public class LockLocationForMaintenanceCommand : ICommand<LocationLockOperationResultDto>, IAuditableCommand
    {
        /// <summary>
        /// 目标库位编码
        /// </summary>
        public string LocationCode { get; }

        /// <summary>
        /// 维护封锁原因（必填项）
        /// </summary>
        public string Reason { get; }

        public string Module => "Location";
        public string Action => "LockForMaintenance";
        public string TargetType => "Location";
        public string? TargetId => LocationCode;
        public string? Description => $"调度员人工设置库位 [{LocationCode}] 进入维护封锁状态";

        public LockLocationForMaintenanceCommand(string locationCode, string reason)
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
