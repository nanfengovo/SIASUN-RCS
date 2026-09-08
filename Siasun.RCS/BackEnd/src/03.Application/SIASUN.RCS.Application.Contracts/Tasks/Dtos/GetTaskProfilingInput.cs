using System;
using Volo.Abp.Application.Dtos;

namespace SIASUN.RCS.Tasks.Dtos
{
    /// <summary>
    /// 分页查询任务步骤剖析列表输入 DTO
    /// </summary>
    public class GetTaskStepListInput : PagedAndSortedResultRequestDto
    {
        /// <summary>任务编号过滤</summary>
        public string? TaskCode { get; set; }

        /// <summary>子系统名称过滤</summary>
        public string? Subsystem { get; set; }

        /// <summary>车辆编号过滤</summary>
        public string? AgvId { get; set; }

        /// <summary>状态过滤 (Success / Failed / Timeout)</summary>
        public string? Status { get; set; }

        /// <summary>起始时间 (UTC)</summary>
        public DateTime? StartTime { get; set; }

        /// <summary>截止时间 (UTC)</summary>
        public DateTime? EndTime { get; set; }
    }

    /// <summary>
    /// 查询任务执行与 Dashboard 指标统计输入 DTO
    /// </summary>
    public class GetMetricsSummaryInput
    {
        /// <summary>统计起始时间 (UTC)</summary>
        public DateTime? StartTime { get; set; }

        /// <summary>统计截止时间 (UTC)</summary>
        public DateTime? EndTime { get; set; }

        /// <summary>工艺区域过滤（可选）</summary>
        public string? Area { get; set; }

        /// <summary>车辆编号过滤（可选）</summary>
        public string? AgvId { get; set; }
    }
}
