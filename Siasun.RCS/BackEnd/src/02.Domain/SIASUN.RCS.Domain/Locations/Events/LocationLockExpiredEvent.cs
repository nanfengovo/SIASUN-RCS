using System;

namespace SIASUN.RCS.Locations.Events
{
    /// <summary>
    /// 库位锁租约超期自愈/报警领域事件
    /// 用于联动 SystemEventLog 与 SignalR 实时诊断监控推送
    /// </summary>
    public class LocationLockExpiredEvent
    {
        /// <summary>
        /// 库位编码
        /// </summary>
        public string LocationCode { get; }

        /// <summary>
        /// 原持锁任务主键
        /// </summary>
        public Guid TaskId { get; }

        /// <summary>
        /// 原绑定的车辆编号
        /// </summary>
        public string VehicleCode { get; }

        /// <summary>
        /// 租约超时时间点（UTC）
        /// </summary>
        public DateTime ExpirationTime { get; }

        /// <summary>
        /// 事件触发时间（UTC）
        /// </summary>
        public DateTime DetectedTime { get; }

        /// <summary>
        /// 初始化超期领域事件
        /// </summary>
        public LocationLockExpiredEvent(
            string locationCode,
            Guid taskId,
            string vehicleCode,
            DateTime expirationTime,
            DateTime detectedTime)
        {
            LocationCode = locationCode;
            TaskId = taskId;
            VehicleCode = vehicleCode;
            ExpirationTime = expirationTime;
            DetectedTime = detectedTime;
        }
    }
}
