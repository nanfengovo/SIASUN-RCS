using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace SIASUN.RCS.Locations
{
    /// <summary>
    /// 物理库位锁持久化实体 (原子排他性安全区)
    /// 保证物理空间独占性，杜绝多车争位碰撞与调度死锁
    /// </summary>
    public class LocationLock : CreationAuditedEntity<Guid>
    {
        /// <summary>
        /// 物理或逻辑库位编码（全局唯一排他索引）
        /// </summary>
        public string LocationCode { get; private set; } = string.Empty;

        /// <summary>
        /// 锁类型（Fetch/Put/Transit/Maintenance）
        /// </summary>
        public LocationLockType LockType { get; private set; }

        /// <summary>
        /// 持有该锁的内部调度任务主键（维护锁时为 Guid.Empty）
        /// </summary>
        public Guid TaskId { get; private set; }

        /// <summary>
        /// 当前绑定的执行车辆编号
        /// </summary>
        public string VehicleCode { get; private set; } = string.Empty;

        /// <summary>
        /// 锁租约过期时间（UTC，超出此时限视为僵尸孤儿锁，支持自愈抢占；维护锁为 DateTime.MaxValue）
        /// </summary>
        public DateTime LeaseExpirationTime { get; private set; }

        /// <summary>
        /// 加锁原因或维护说明
        /// </summary>
        public string? Reason { get; private set; }

        /// <summary>
        /// EF Core 所需受保护构造函数
        /// </summary>
        protected LocationLock()
        {
        }

        /// <summary>
        /// 创建常规作业库位锁定记录
        /// </summary>
        /// <param name="id">锁实体主键</param>
        /// <param name="locationCode">库位编码</param>
        /// <param name="lockType">锁类型</param>
        /// <param name="taskId">任务ID</param>
        /// <param name="vehicleCode">车辆编号</param>
        /// <param name="leaseDuration">租约保护时长</param>
        /// <param name="reason">锁定原因说明</param>
        public LocationLock(
            Guid id,
            string locationCode,
            LocationLockType lockType,
            Guid taskId,
            string vehicleCode,
            TimeSpan leaseDuration,
            string? reason = null) : base(id)
        {
            LocationCode = Check.NotNullOrWhiteSpace(locationCode, nameof(locationCode), maxLength: LocationLockConsts.MaxLocationCodeLength);
            LockType = lockType;
            TaskId = taskId;
            VehicleCode = Check.NotNullOrWhiteSpace(vehicleCode, nameof(vehicleCode), maxLength: LocationLockConsts.MaxVehicleCodeLength);
            Reason = reason;

            if (lockType == LocationLockType.Maintenance)
            {
                LeaseExpirationTime = DateTime.MaxValue;
            }
            else
            {
                LeaseExpirationTime = DateTime.UtcNow.Add(leaseDuration);
            }
        }

        /// <summary>
        /// 创建人工维护封锁记录
        /// </summary>
        /// <param name="id">锁实体主键</param>
        /// <param name="locationCode">库位编码</param>
        /// <param name="reason">维护封锁原因</param>
        /// <returns>维护锁实体实例</returns>
        public static LocationLock CreateMaintenanceLock(Guid id, string locationCode, string reason)
        {
            return new LocationLock(
                id,
                locationCode,
                LocationLockType.Maintenance,
                Guid.Empty,
                LocationLockConsts.MaintenanceVehicleCode,
                TimeSpan.Zero,
                reason);
        }

        /// <summary>
        /// 判断当前锁是否已经超过租约时效（人工维护锁永不超时）
        /// </summary>
        /// <param name="nowUtc">当前 UTC 时间戳</param>
        /// <returns>是否已超期</returns>
        public bool IsExpired(DateTime nowUtc)
        {
            if (LockType == LocationLockType.Maintenance)
            {
                return false;
            }

            return nowUtc > LeaseExpirationTime;
        }

        /// <summary>
        /// 租约延长续期（长距离行驶或避障拥堵时延长锁定时长）
        /// </summary>
        /// <param name="duration">延长时长</param>
        public void ExtendLease(TimeSpan duration)
        {
            if (LockType != LocationLockType.Maintenance)
            {
                LeaseExpirationTime = DateTime.UtcNow.Add(duration);
            }
        }

        /// <summary>
        /// 覆盖更新锁信息（用于僵尸锁自愈接管）
        /// </summary>
        /// <param name="newTaskId">新接管任务ID</param>
        /// <param name="newVehicleCode">新执行车辆编号</param>
        /// <param name="newLockType">新锁类型</param>
        /// <param name="leaseDuration">新租约时长</param>
        /// <param name="reason">接管说明</param>
        public void Reclaim(
            Guid newTaskId,
            string newVehicleCode,
            LocationLockType newLockType,
            TimeSpan leaseDuration,
            string? reason = null)
        {
            TaskId = newTaskId;
            VehicleCode = Check.NotNullOrWhiteSpace(newVehicleCode, nameof(newVehicleCode), maxLength: LocationLockConsts.MaxVehicleCodeLength);
            LockType = newLockType;
            Reason = reason;

            if (newLockType == LocationLockType.Maintenance)
            {
                LeaseExpirationTime = DateTime.MaxValue;
            }
            else
            {
                LeaseExpirationTime = DateTime.UtcNow.Add(leaseDuration);
            }
        }
    }
}
