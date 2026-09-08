using System.ComponentModel.DataAnnotations;

namespace SIASUN.RCS.TM.Dtos
{
    /// <summary>
    /// TM 底盘回调报文入参
    /// </summary>
    public class TmCallbackInput
    {
        /// <summary>
        /// 底层 TM 报文任务流水号（对应 TaskSerialMapping 中的 TmSerial）
        /// </summary>
        [Required]
        public string TmSerial { get; set; } = string.Empty;

        /// <summary>
        /// 执行状态（"Completed", "Failed", "Executing"）
        /// </summary>
        [Required]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 执行该动作的 AGV 编号
        /// </summary>
        public string? VehicleCode { get; set; }

        /// <summary>
        /// 异常错误代码（若失败）
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// 异常错误描述（若失败）
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 动作完成或当前携带的扩展载荷 JSON
        /// </summary>
        public string? Payload { get; set; }
    }
}
