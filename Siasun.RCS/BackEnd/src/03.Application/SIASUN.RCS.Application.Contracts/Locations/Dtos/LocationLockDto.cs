using SIASUN.RCS.Commands;
using System;

namespace SIASUN.RCS.Locations.Dtos
{
    /// <summary>
    /// 库位锁状态数据传输对象
    /// </summary>
    public class LocationLockDto
    {
        /// <summary>
        /// 库位编码
        /// </summary>
        public string LocationCode { get; set; } = string.Empty;

        /// <summary>
        /// 锁类型（Fetch/Put/Transit/Maintenance）
        /// </summary>
        public LocationLockType LockType { get; set; }

        /// <summary>
        /// 持有任务ID
        /// </summary>
        public Guid TaskId { get; set; }

        /// <summary>
        /// 执行小车编号
        /// </summary>
        public string VehicleCode { get; set; } = string.Empty;

        /// <summary>
        /// 加锁时间（UTC）
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// 租约过期时间（UTC）
        /// </summary>
        public DateTime LeaseExpirationTime { get; set; }

        /// <summary>
        /// 加锁原因或维护说明
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// 是否处于人工维护封锁状态
        /// </summary>
        public bool IsMaintenance => LockType == LocationLockType.Maintenance;

        /// <summary>
        /// 当前是否已超期（僵尸锁）
        /// </summary>
        public bool IsExpired => LockType != LocationLockType.Maintenance && DateTime.UtcNow > LeaseExpirationTime;
    }

    /// <summary>
    /// 库位锁人工干预操作统一结果 DTO（实现 IStateTransitionResult 以便自动提取变迁审计）
    /// </summary>
    public class LocationLockOperationResultDto : IStateTransitionResult
    {
        /// <summary>
        /// 操作是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 操作涉及的库位编码
        /// </summary>
        public string LocationCode { get; set; } = string.Empty;

        /// <summary>
        /// 结果提示信息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 受影响的原持锁任务ID（若有）
        /// </summary>
        public Guid? AffectedTaskId { get; set; }

        /// <summary>
        /// 变迁前状态快照
        /// </summary>
        public string? BeforeState { get; set; }

        /// <summary>
        /// 变迁后状态快照
        /// </summary>
        public string? AfterState { get; set; }

        /// <summary>
        /// 目标对象类型
        /// </summary>
        public string? TargetType => "Location";

        /// <summary>
        /// 目标对象唯一标识
        /// </summary>
        public string? TargetId => LocationCode;

        /// <summary>
        /// 构造成功结果
        /// </summary>
        public static LocationLockOperationResultDto Ok(
            string locationCode,
            string message,
            Guid? affectedTaskId = null,
            string? beforeState = null,
            string? afterState = null)
        {
            return new LocationLockOperationResultDto
            {
                Success = true,
                LocationCode = locationCode,
                Message = message,
                AffectedTaskId = affectedTaskId,
                BeforeState = beforeState,
                AfterState = afterState
            };
        }

        /// <summary>
        /// 构造失败结果
        /// </summary>
        public static LocationLockOperationResultDto Fail(
            string locationCode,
            string message,
            string? beforeState = null,
            string? afterState = null)
        {
            return new LocationLockOperationResultDto
            {
                Success = false,
                LocationCode = locationCode,
                Message = message,
                BeforeState = beforeState,
                AfterState = afterState
            };
        }
    }
}
