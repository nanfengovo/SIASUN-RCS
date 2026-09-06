using System.ComponentModel.DataAnnotations;

namespace SIASUN.RCS.Dispatch.Dtos
{
    /// <summary>
    /// 调度员人工强制完结任务入参
    /// </summary>
    public class ForceEndTaskInput
    {
        /// <summary>
        /// 待强制完结的任务唯一编号
        /// </summary>
        [Required]
        public string TaskId { get; set; } = string.Empty;

        /// <summary>
        /// 执行此任务的 AGV 车体编号（可选）
        /// </summary>
        public string? AgvId { get; set; }

        /// <summary>
        /// 强制完结原因（必填，定分止争关键凭证）
        /// </summary>
        [Required]
        public string Reason { get; set; } = string.Empty;
    }
}

