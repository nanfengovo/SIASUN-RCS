using System.ComponentModel.DataAnnotations;

namespace SIASUN.RCS.Dispatch.Dtos
{
    /// <summary>
    /// 调度员人工恢复失败任务入参（断点重试/显式状态复原）
    /// </summary>
    public class ResumeTaskInput
    {
        /// <summary>
        /// 待恢复的任务编号或主键 ID
        /// </summary>
        [Required]
        public string TaskId { get; set; } = string.Empty;

        /// <summary>
        /// 是否重试当前失败步骤（true: 保持当前 StepIndex 重试；false: 跳过当前失败步骤推进至下一步）
        /// </summary>
        public bool RetryCurrentStep { get; set; } = true;

        /// <summary>
        /// 执行此任务的 AGV 车体编号（可选）
        /// </summary>
        public string? AgvId { get; set; }

        /// <summary>
        /// 人工干预恢复原因说明（必填，定分止争关键凭证）
        /// </summary>
        [Required]
        public string Reason { get; set; } = string.Empty;
    }
}
