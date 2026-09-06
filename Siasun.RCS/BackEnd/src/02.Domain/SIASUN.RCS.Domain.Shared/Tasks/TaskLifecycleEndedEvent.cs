using System;

namespace SIASUN.RCS.Tasks
{
    /// <summary>
    /// AGV 调度任务生命周期完结领域事件（支持 Succeeded / Failed / Canceled 异步解耦副作用）
    /// 严格遵循 SIASUN RCS 铁律 7：严禁在实体或工作流步进中直接过程式调用跨领域副作用，必须通过领域事件解耦
    /// </summary>
    public class TaskLifecycleEndedEvent
    {
        /// <summary>
        /// 调度任务全局唯一主键
        /// </summary>
        public Guid TaskId { get; set; }

        /// <summary>
        /// 业务任务编号
        /// </summary>
        public string TaskCode { get; set; } = string.Empty;

        /// <summary>
        /// 完结时的最终生命周期状态（Succeeded / Failed / Canceled）
        /// </summary>
        public AgvTaskStatus FinalStatus { get; set; }

        /// <summary>
        /// 失败、取消或强制完结的原因说明
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// 全链路追踪 TraceId
        /// </summary>
        public string? TraceId { get; set; }

        /// <summary>
        /// 指派执行的 AGV 车辆编号
        /// </summary>
        public string? AssignedVehicleCode { get; set; }

        /// <summary>
        /// 任务最终结束时间（UTC）
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 反序列化构造函数
        /// </summary>
        public TaskLifecycleEndedEvent()
        {
        }

        /// <summary>
        /// 构造包含全量生命周期结束元数据的领域事件
        /// </summary>
        /// <param name="taskId">任务主键</param>
        /// <param name="taskCode">业务任务编号</param>
        /// <param name="finalStatus">最终生命周期状态</param>
        /// <param name="reason">原因描述</param>
        /// <param name="traceId">追踪 TraceId</param>
        /// <param name="assignedVehicleCode">指派车辆编号</param>
        /// <param name="endTime">完结时间</param>
        public TaskLifecycleEndedEvent(
            Guid taskId,
            string taskCode,
            AgvTaskStatus finalStatus,
            string? reason,
            string? traceId,
            string? assignedVehicleCode,
            DateTime endTime)
        {
            TaskId = taskId;
            TaskCode = taskCode;
            FinalStatus = finalStatus;
            Reason = reason;
            TraceId = traceId;
            AssignedVehicleCode = assignedVehicleCode;
            EndTime = endTime;
        }
    }
}
