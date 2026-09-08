using System.ComponentModel.DataAnnotations;

namespace SIASUN.RCS.Dispatch.Dtos
{
    /// <summary>
    /// 调度员人工回滚并重试任务入参（SAGA 补偿与安全回退）
    /// </summary>
    public class RollbackAndRetryInput
    {
        /// <summary>
        /// 待回滚的任务编号或主键 ID
        /// </summary>
        [Required]
        public string TaskId { get; set; } = string.Empty;

        /// <summary>
        /// 目标回滚步骤索引（必须小于等于当前任务步骤索引）
        /// </summary>
        [Required]
        [Range(0, 100)]
        public int TargetStepIndex { get; set; }

        /// <summary>
        /// 执行此任务的 AGV 车体编号（可选）
        /// </summary>
        public string? AgvId { get; set; }

        /// <summary>
        /// 人工干预回滚原因说明（必填，定分止争关键凭证）
        /// </summary>
        [Required]
        public string Reason { get; set; } = string.Empty;
    }
}
