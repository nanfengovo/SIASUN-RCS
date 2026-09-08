using System;

namespace SIASUN.RCS.TM.Dtos
{
    /// <summary>
    /// TM 回调处理结果
    /// </summary>
    public class TmCallbackResultDto
    {
        /// <summary>
        /// 是否成功匹配并处理回调
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 匹配到的内部 AGV 任务编号
        /// </summary>
        public string? TaskCode { get; set; }

        /// <summary>
        /// 匹配到的内部航段
        /// </summary>
        public string? Leg { get; set; }

        /// <summary>
        /// 处理后的任务生命周期状态
        /// </summary>
        public string? CurrentTaskStatus { get; set; }

        /// <summary>
        /// 推进后的工作流步骤序号
        /// </summary>
        public int? CurrentStepIndex { get; set; }

        /// <summary>
        /// 业务提示消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 处理时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
