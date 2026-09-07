using System;

namespace SIASUN.RCS.Locations
{
    /// <summary>
    /// 单个库位加锁请求值对象
    /// </summary>
    public class LocationLockRequest
    {
        /// <summary>
        /// 目标库位或管控区域编码
        /// </summary>
        public string LocationCode { get; }

        /// <summary>
        /// 申请的锁类型（Fetch/Put/Transit）
        /// </summary>
        public LocationLockType LockType { get; }

        /// <summary>
        /// 期望租约有效时长（默认 30 分钟）
        /// </summary>
        public TimeSpan LeaseDuration { get; }

        /// <summary>
        /// 初始化库位加锁请求
        /// </summary>
        /// <param name="locationCode">库位编码</param>
        /// <param name="lockType">锁类型</param>
        /// <param name="leaseDuration">租约保护时长（可选，默认 30 分钟）</param>
        public LocationLockRequest(string locationCode, LocationLockType lockType, TimeSpan? leaseDuration = null)
        {
            LocationCode = string.IsNullOrWhiteSpace(locationCode)
                ? throw new ArgumentNullException(nameof(locationCode))
                : locationCode.Trim();
            LockType = lockType;
            LeaseDuration = leaseDuration ?? TimeSpan.FromMinutes(30);
        }
    }

    /// <summary>
    /// 库位加锁执行结果值对象
    /// </summary>
    public class LocationLockResult
    {
        /// <summary>
        /// 是否全部抢占成功
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// 冲突的库位编码（仅失败时有值）
        /// </summary>
        public string? ConflictedLocation { get; }

        /// <summary>
        /// 当前持有该库位的冲突任务主键（仅失败时有值）
        /// </summary>
        public Guid? ConflictedTaskId { get; }

        /// <summary>
        /// 当前持有该库位的车辆编号（仅失败时有值）
        /// </summary>
        public string? ConflictedVehicleCode { get; }

        /// <summary>
        /// 失败原因说明
        /// </summary>
        public string? FailureReason { get; }

        private LocationLockResult(
            bool isSuccess,
            string? conflictedLocation = null,
            Guid? conflictedTaskId = null,
            string? conflictedVehicleCode = null,
            string? failureReason = null)
        {
            IsSuccess = isSuccess;
            ConflictedLocation = conflictedLocation;
            ConflictedTaskId = conflictedTaskId;
            ConflictedVehicleCode = conflictedVehicleCode;
            FailureReason = failureReason;
        }

        /// <summary>
        /// 快速构建成功结果
        /// </summary>
        public static LocationLockResult Success() => new(true);

        /// <summary>
        /// 构建冲突失败结果
        /// </summary>
        /// <param name="location">冲突库位编码</param>
        /// <param name="taskId">当前持锁任务</param>
        /// <param name="vehicleCode">当前持锁车辆</param>
        /// <param name="reason">冲突原因说明</param>
        /// <returns>失败结果实例</returns>
        public static LocationLockResult Conflict(string location, Guid taskId, string vehicleCode, string reason)
            => new(false, location, taskId, vehicleCode, reason);
    }
}
