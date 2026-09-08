using System.Collections.Generic;

namespace SIASUN.RCS.Hardware
{
    /// <summary>
    /// 硬件门禁与现场设备交互上下文参数
    /// </summary>
    public class HardwareGateContext
    {
        /// <summary>
        /// 目标硬件设备编号或 PLC 标识（例如 "AIR_SHOWER_DOOR_01", "PLC_STK_01"）
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// 关联库位编号（如有，例如 "SLOT_A_101"）
        /// </summary>
        public string? LocationCode { get; set; }

        /// <summary>
        /// 关联车辆编号（如有，例如 "AGV_01"）
        /// </summary>
        public string? VehicleCode { get; set; }

        /// <summary>
        /// 关联调度任务编号（如有，例如 "TASK_20260908_001"）
        /// </summary>
        public string? TaskCode { get; set; }

        /// <summary>
        /// 全链路追踪 TraceId
        /// </summary>
        public string? TraceId { get; set; }

        /// <summary>
        /// 自定义扩展参数字典
        /// </summary>
        public IDictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public HardwareGateContext()
        {
        }

        /// <summary>
        /// 便捷构造函数
        /// </summary>
        /// <param name="deviceId">硬件设备标识</param>
        /// <param name="locationCode">库位编号</param>
        /// <param name="vehicleCode">车辆编号</param>
        /// <param name="taskCode">任务编号</param>
        /// <param name="traceId">追踪 TraceId</param>
        public HardwareGateContext(
            string deviceId,
            string? locationCode = null,
            string? vehicleCode = null,
            string? taskCode = null,
            string? traceId = null)
        {
            DeviceId = deviceId;
            LocationCode = locationCode;
            VehicleCode = vehicleCode;
            TaskCode = taskCode;
            TraceId = traceId;
        }
    }
}
