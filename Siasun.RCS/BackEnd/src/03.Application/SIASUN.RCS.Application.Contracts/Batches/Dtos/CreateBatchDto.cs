using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SIASUN.RCS.Batches.Dtos
{
    /// <summary>
    /// 创建 AGV 搬运批次请求 DTO
    /// </summary>
    public class CreateBatchDto
    {
        /// <summary>
        /// 批次编号（例如 "BATCH_20260908_001"）
        /// </summary>
        [Required]
        [StringLength(64)]
        public string BatchCode { get; set; } = string.Empty;

        /// <summary>
        /// 起始工位（如 "STK_PORT_01"）
        /// </summary>
        [Required]
        [StringLength(64)]
        public string SourceStation { get; set; } = string.Empty;

        /// <summary>
        /// 目标工位（如 "MOLDING_IN_02"）
        /// </summary>
        [Required]
        [StringLength(64)]
        public string TargetStation { get; set; } = string.Empty;

        /// <summary>
        /// 包含的载具/FOUP 清单
        /// </summary>
        [Required]
        public List<string> CarrierCodes { get; set; } = new();

        /// <summary>
        /// 选用的轻量工作流定义标识（默认 "transfer_standard"）
        /// </summary>
        public string WorkflowKey { get; set; } = "transfer_standard";

        /// <summary>
        /// 全链路追踪 TraceId
        /// </summary>
        [StringLength(64)]
        public string? TraceId { get; set; }

        /// <summary>
        /// 备注说明
        /// </summary>
        [StringLength(512)]
        public string? Remark { get; set; }
    }
}
