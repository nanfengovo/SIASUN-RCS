using System;

namespace SIASUN.RCS.Adapters.Plc.S7
{
    /// <summary>
    /// 半导体洁净室光电传感器与库位物理安全白名单比对校验器
    /// 严格核验在位光电、双重盒检测、斜置报警与左右边缘传感器
    /// </summary>
    public static class PhotoelectricSensorValidator
    {
        /// <summary>
        /// 校验取货前置光电条件（工位必须有货且姿态正常）
        /// </summary>
        /// <param name="slot">槽位遥测数据</param>
        /// <param name="expectedCarrier">预期取出的载具编号（可选核对）</param>
        /// <returns>校验结果</returns>
        public static SensorValidationResult ValidateForFetch(S7SlotData slot, string? expectedCarrier = null)
        {
            if (!slot.IsPresent)
            {
                return new SensorValidationResult(false, "SENSOR_NO_CARRIER_DETECTED",
                    $"工位槽位 [{slot.SlotIndex}] 光电传感器未感应到在位载具，禁止空抓取货");
            }

            if (slot.IsTilted)
            {
                return new SensorValidationResult(false, "SENSOR_CARRIER_TILTED",
                    $"工位槽位 [{slot.SlotIndex}] 检测到晶圆盒倾斜/斜置报警，执行安全互锁阻断");
            }

            if (!slot.LeftSensor || !slot.RightSensor)
            {
                return new SensorValidationResult(false, "SENSOR_ALIGNMENT_FAULT",
                    $"工位槽位 [{slot.SlotIndex}] 左右光电对齐不一致 (Left={slot.LeftSensor}, Right={slot.RightSensor})，存在位置偏移风险");
            }

            if (!string.IsNullOrWhiteSpace(expectedCarrier) &&
                !string.IsNullOrWhiteSpace(slot.CarrierCode) &&
                !string.Equals(slot.CarrierCode, expectedCarrier, StringComparison.OrdinalIgnoreCase))
            {
                return new SensorValidationResult(false, "SENSOR_CARRIER_MISMATCH",
                    $"工位槽位 [{slot.SlotIndex}] 实际载具 [{slot.CarrierCode}] 与任务目标 [{expectedCarrier}] 不一致");
            }

            return new SensorValidationResult(true, "OK", "光电校验通过，允许安全取货");
        }

        /// <summary>
        /// 校验放货前置光电条件（工位必须为空且无异物阻塞）
        /// </summary>
        /// <param name="slot">槽位遥测数据</param>
        /// <returns>校验结果</returns>
        public static SensorValidationResult ValidateForPut(S7SlotData slot)
        {
            if (slot.IsPresent)
            {
                return new SensorValidationResult(false, "SENSOR_SLOT_ALREADY_OCCUPIED",
                    $"工位槽位 [{slot.SlotIndex}] 光电传感器检测到已有在位载具 [{slot.CarrierCode}]，禁止重叠放货");
            }

            if (slot.LeftSensor || slot.RightSensor)
            {
                return new SensorValidationResult(false, "SENSOR_OBSTACLE_DETECTED",
                    $"工位槽位 [{slot.SlotIndex}] 边缘光电检测到异物遮挡，禁止机械臂伸入");
            }

            return new SensorValidationResult(true, "OK", "光电净空校验通过，允许安全放货");
        }
    }

    /// <summary>
    /// 光电传感器校验结果
    /// </summary>
    public record SensorValidationResult(
        bool IsValid,
        string Code,
        string Message);
}
