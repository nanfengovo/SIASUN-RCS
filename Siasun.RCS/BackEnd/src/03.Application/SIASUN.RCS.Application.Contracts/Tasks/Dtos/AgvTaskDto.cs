using System;
using SIASUN.RCS.Tasks;
using Volo.Abp.Application.Dtos;

namespace SIASUN.RCS.Tasks.Dtos
{
    /// <summary>
    /// AGV 调度任务基础 DTO
    /// </summary>
    public class AgvTaskDto : FullAuditedEntityDto<Guid>
    {
        /// <summary>
        /// 任务唯一业务编号
        /// </summary>
        public string TaskCode { get; set; } = string.Empty;

        /// <summary>
        /// 任务状态
        /// </summary>
        public AgvTaskStatus Status { get; set; }

        /// <summary>
        /// 起始工位编号
        /// </summary>
        public string? FromStation { get; set; }

        /// <summary>
        /// 目标工位编号
        /// </summary>
        public string? ToStation { get; set; }

        /// <summary>
        /// 载具/FOUP 晶圆盒编号
        /// </summary>
        public string? CarrierCode { get; set; }

        /// <summary>
        /// 关联批次编号
        /// </summary>
        public string? BatchId { get; set; }

        /// <summary>
        /// 指派的 AGV 编号
        /// </summary>
        public string? AssignedVehicleCode { get; set; }

        /// <summary>
        /// 绑定的工作流标识
        /// </summary>
        public string? WorkflowKey { get; set; }

        /// <summary>
        /// 当前细粒度步进索引
        /// </summary>
        public int StepIndex { get; set; }

        /// <summary>
        /// 全链路追踪 TraceId
        /// </summary>
        public string? TraceId { get; set; }
    }
}
