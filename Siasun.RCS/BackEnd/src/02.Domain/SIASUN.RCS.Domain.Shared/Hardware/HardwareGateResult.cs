using System.Collections.Generic;

namespace SIASUN.RCS.Hardware
{
    /// <summary>
    /// 工业硬件安全门禁与互锁校验执行结果
    /// 遵循《AGENTS.md》铁律 4：硬件与现场设备统一抽象在 IHardwareGate 端口之后
    /// </summary>
    public class HardwareGateResult
    {
        /// <summary>
        /// 是否校验通过或执行成功
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// 状态码或错误标识（如 "OK", "INTERLOCK_BLOCKED", "DOOR_OPEN_TIMEOUT", "SENSOR_OCCUPIED"）
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// 描述信息
        /// </summary>
        public string? Message { get; }

        /// <summary>
        /// 附加遥测或传感器上下文细节
        /// </summary>
        public IReadOnlyDictionary<string, object?>? Details { get; }

        private HardwareGateResult(bool isSuccess, string code, string? message, IReadOnlyDictionary<string, object?>? details)
        {
            IsSuccess = isSuccess;
            Code = code;
            Message = message;
            Details = details;
        }

        /// <summary>
        /// 创建操作成功的门禁结果
        /// </summary>
        /// <param name="message">成功说明</param>
        /// <param name="details">附加数据</param>
        /// <returns>成功的硬件门禁结果</returns>
        public static HardwareGateResult Success(string? message = null, IReadOnlyDictionary<string, object?>? details = null)
        {
            return new HardwareGateResult(true, "OK", message ?? "硬件门禁条件已满足", details);
        }

        /// <summary>
        /// 创建操作失败的门禁结果
        /// </summary>
        /// <param name="code">错误码</param>
        /// <param name="message">失败原因</param>
        /// <param name="details">附加数据</param>
        /// <returns>失败的硬件门禁结果</returns>
        public static HardwareGateResult Failed(string code, string message, IReadOnlyDictionary<string, object?>? details = null)
        {
            return new HardwareGateResult(false, code, message, details);
        }
    }
}
