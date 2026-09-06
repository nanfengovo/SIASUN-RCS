using System.ComponentModel.DataAnnotations;

namespace SIASUN.RCS.Dispatch.Dtos
{
    /// <summary>
    /// 调度员人工干预取消任务入参
    /// </summary>
    public class CancelTaskInput
    {
        /// <summary>
        /// 待取消的任务唯一编号（如 TASK-20260901-001）
        /// </summary>
        [Required]
        public string TaskId { get; set; } = string.Empty;

        /// <summary>
        /// 执行此任务的 AGV 车体编号（可选，若已知）
        /// </summary>
        public string? AgvId { get; set; }

        /// <summary>
        /// 人工干预取消原因（必填，定分止争与事故追溯关键凭证）
        /// </summary>
        [Required]
        public string Reason { get; set; } = string.Empty;
    }
}

