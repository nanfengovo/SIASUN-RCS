using System;
using Volo.Abp.Application.Dtos;

namespace SIASUN.RCS.Batches.Dtos
{
    /// <summary>
    /// AGV 搬运批次详情 DTO
    /// </summary>
    public class AgvBatchDto : FullAuditedEntityDto<Guid>
    {
        /// <summary>
        /// 批次编号
        /// </summary>
        public string BatchCode { get; set; } = string.Empty;

        /// <summary>
        /// 批次状态
        /// </summary>
        public AgvBatchStatus Status { get; set; }

        /// <summary>
        /// 子任务总数
        /// </summary>
        public int TotalSubTasks { get; set; }

        /// <summary>
        /// 已成功子任务数量
        /// </summary>
        public int CompletedSubTasks { get; set; }

        /// <summary>
        /// 失败子任务数量
        /// </summary>
        public int FailedSubTasks { get; set; }

        /// <summary>
        /// 载具清单
        /// </summary>
        public string CarrierCodes { get; set; } = string.Empty;

        /// <summary>
        /// 起始工位
        /// </summary>
        public string SourceStation { get; set; } = string.Empty;

        /// <summary>
        /// 目标工位
        /// </summary>
        public string TargetStation { get; set; } = string.Empty;

        /// <summary>
        /// 全链路追踪 TraceId
        /// </summary>
        public string? TraceId { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }
    }
}
