using System.ComponentModel.DataAnnotations;

namespace SIASUN.RCS.Dispatch.Dtos
{
    /// <summary>
    /// 调度员人工指定/指派车辆入参
    /// </summary>
    public class AssignVehicleInput
    {
        /// <summary>
        /// 待指派的调度任务唯一编号
        /// </summary>
        [Required]
        public string TaskId { get; set; } = string.Empty;

        /// <summary>
        /// 目标 AGV 车体唯一编号（如 AGV-01）
        /// </summary>
        [Required]
        public string AgvId { get; set; } = string.Empty;

        /// <summary>
        /// 人工指定车辆原因
        /// </summary>
        [Required]
        public string Reason { get; set; } = string.Empty;
    }
}

