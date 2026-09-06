using System.ComponentModel.DataAnnotations;

namespace SIASUN.RCS.Dispatch.Dtos
{
    /// <summary>
    /// 调度员人工复位车辆状态入参
    /// </summary>
    public class ResetVehicleInput
    {
        /// <summary>
        /// 目标 AGV 车体唯一编号（如 AGV-01）
        /// </summary>
        [Required]
        public string AgvId { get; set; } = string.Empty;

        /// <summary>
        /// 复位原因或处置依据（人工干预必填）
        /// </summary>
        [Required]
        public string Reason { get; set; } = string.Empty;
    }
}

