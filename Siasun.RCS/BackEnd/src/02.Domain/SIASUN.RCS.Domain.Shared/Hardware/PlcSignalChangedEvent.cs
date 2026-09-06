using System;

namespace SIASUN.RCS.Hardware
{
    /// <summary>
    /// PLC 信号跳变（Edge Trigger）变位领域事件
    /// 遵循《AGENTS.md》铁律 4 与铁律 7：工控硬件与现场设备通信纯插件化隔离，高频轮询仅在信号跳变时发布领域事件，严禁逐帧刷库
    /// </summary>
    public class PlcSignalChangedEvent
    {
        /// <summary>
        /// 目标硬件设备标识（例如 "PLC-01", "STK-PLC"）
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// 点位/信号名称（例如 "TwinArm_Clearance", "Photoelectric_Sensor_01"）
        /// </summary>
        public string TagName { get; set; } = string.Empty;

        /// <summary>
        /// 变位前旧值
        /// </summary>
        public object? OldValue { get; set; }

        /// <summary>
        /// 变位后新值
        /// </summary>
        public object? NewValue { get; set; }

        /// <summary>
        /// 信号数据类型或类别（例如 "Boolean", "Integer", "AlarmBit"）
        /// </summary>
        public string? DataType { get; set; }

        /// <summary>
        /// 变位发生时间戳 (UTC)
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 全链路追踪 TraceId (可选)
        /// </summary>
        public string? TraceId { get; set; }

        /// <summary>
        /// 关联的车辆编号 (如有)
        /// </summary>
        public string? VehicleCode { get; set; }

        /// <summary>
        /// 关联的任务编号 (如有)
        /// </summary>
        public string? TaskCode { get; set; }

        /// <summary>
        /// 无参构造函数 (支持反序列化)
        /// </summary>
        public PlcSignalChangedEvent()
        {
        }

        /// <summary>
        /// 构造包含基础信号跳变元数据的 PLC 变位领域事件
        /// </summary>
        /// <param name="deviceId">目标硬件设备标识</param>
        /// <param name="tagName">点位名称</param>
        /// <param name="oldValue">旧值</param>
        /// <param name="newValue">新值</param>
        /// <param name="dataType">数据类型</param>
        /// <param name="traceId">追踪 TraceId</param>
        /// <param name="vehicleCode">关联车辆编号</param>
        /// <param name="taskCode">关联任务编号</param>
        public PlcSignalChangedEvent(
            string deviceId,
            string tagName,
            object? oldValue,
            object? newValue,
            string? dataType = null,
            string? traceId = null,
            string? vehicleCode = null,
            string? taskCode = null)
        {
            DeviceId = deviceId;
            TagName = tagName;
            OldValue = oldValue;
            NewValue = newValue;
            DataType = dataType;
            TraceId = traceId;
            VehicleCode = vehicleCode;
            TaskCode = taskCode;
            Timestamp = DateTime.UtcNow;
        }
    }
}

