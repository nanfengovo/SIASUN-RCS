using System;
using Volo.Abp.Application.Dtos;

namespace SIASUN.RCS.Tasks.Dtos
{
    /// <summary>
    /// 任务步骤剖析数据传输对象
    /// </summary>
    public class TaskStepProfilingDto : EntityDto<Guid>
    {
        /// <summary>关联的任务唯一编号</summary>
        public string TaskCode { get; set; } = string.Empty;

        /// <summary>关联的批次号或子批次标识</summary>
        public string? BatchId { get; set; }

        /// <summary>关联的执行车辆编号</summary>
        public string? AgvId { get; set; }

        /// <summary>工作流步进索引 (StepIndex)</summary>
        public int StepIndex { get; set; }

        /// <summary>激活程段标识（如 Fetch / Put）</summary>
        public string? ActiveLeg { get; set; }

        /// <summary>子系统类别 (AMA / Mica / PLC / LocationLock / TM / Arm / Vision / TrafficControl)</summary>
        public string Subsystem { get; set; } = string.Empty;

        /// <summary>操作动作名称</summary>
        public string OperationName { get; set; } = string.Empty;

        /// <summary>开始时间 (UTC)</summary>
        public DateTime StartTime { get; set; }

        /// <summary>结束时间 (UTC)</summary>
        public DateTime EndTime { get; set; }

        /// <summary>执行消耗时间（毫秒）</summary>
        public long DurationMs { get; set; }

        /// <summary>执行状态 (Success / Failed / Timeout)</summary>
        public string Status { get; set; } = "Success";

        /// <summary>业务摘要说明</summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>详细报文或参数上下文</summary>
        public string? Details { get; set; }

        /// <summary>全链路追踪 TraceId</summary>
        public string? TraceId { get; set; }
    }
}
