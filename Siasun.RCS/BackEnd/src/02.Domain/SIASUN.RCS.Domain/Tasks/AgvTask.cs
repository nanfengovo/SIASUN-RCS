using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace SIASUN.RCS.Tasks
{
    /// <summary>
    /// AGV 调度任务聚合根（严格遵循 SIASUN RCS 5 状态粗粒度生命周期规范）
    /// </summary>
    public class AgvTask : FullAuditedAggregateRoot<Guid>
    {
        /// <summary>
        /// 业务任务编号（不可重复）
        /// </summary>
        public string TaskCode { get; private set; } = string.Empty;

        /// <summary>
        /// 粗粒度生命周期状态（Pending / Running / Succeeded / Failed / Canceled）
        /// </summary>
        public AgvTaskStatus Status { get; private set; } = AgvTaskStatus.Pending;

        /// <summary>
        /// 任务内部轻量工作流步进索引（细粒度推进核心）
        /// </summary>
        public int StepIndex { get; private set; }

        /// <summary>
        /// 异步等待事件/信号（步进阻塞标识，如等待 PLC 门到位、等待 TM 完成回调）
        /// </summary>
        public string? WaitingEvent { get; private set; }

        /// <summary>
        /// 当前激活的多程段标识（例如 "Fetch" / "Put" / "Park"）
        /// </summary>
        public string? ActiveLeg { get; private set; }

        /// <summary>
        /// 指派执行该任务的 AGV 实体 ID
        /// </summary>
        public Guid? AssignedVehicleId { get; private set; }

        /// <summary>
        /// 指派执行该任务的 AGV 编号（例如 "AGV-01"）
        /// </summary>
        public string? AssignedVehicleCode { get; private set; }

        /// <summary>
        /// 起始工位编号
        /// </summary>
        public string? FromStation { get; private set; }

        /// <summary>
        /// 目标工位编号
        /// </summary>
        public string? ToStation { get; private set; }

        /// <summary>
        /// 载具/FOUP 晶圆盒编号
        /// </summary>
        public string? CarrierCode { get; private set; }

        /// <summary>
        /// 批次号（支持批次编排与多车协同）
        /// </summary>
        public string? BatchId { get; private set; }

        /// <summary>
        /// 经 Schema 驱动编解码的 OptionCode 指令串
        /// </summary>
        public string? OptionCode { get; private set; }

        /// <summary>
        /// 全链路贯穿 TraceId
        /// </summary>
        public string? TraceId { get; private set; }

        /// <summary>
        /// 失败或取消原因描述
        /// </summary>
        public string? FailureReason { get; private set; }

        /// <summary>
        /// 任务实际开始执行时间（UTC）
        /// </summary>
        public DateTime? StartTime { get; private set; }

        /// <summary>
        /// 任务最终结束时间（UTC，成功/失败/取消）
        /// </summary>
        public DateTime? EndTime { get; private set; }

        /// <summary>
        /// EF Core 内部反序列化受保护无参构造函数
        /// </summary>
        protected AgvTask()
        {
        }

        /// <summary>
        /// 创建新的 AGV 调度任务实例
        /// </summary>
        /// <param name="id">任务全局唯一主键</param>
        /// <param name="taskCode">业务任务编号</param>
        /// <param name="fromStation">起始工位编号</param>
        /// <param name="toStation">目标工位编号</param>
        /// <param name="carrierCode">载具编号</param>
        /// <param name="batchId">批次号</param>
        /// <param name="optionCode">OptionCode 指令串</param>
        /// <param name="traceId">链路追踪 TraceId</param>
        public AgvTask(
            Guid id,
            string taskCode,
            string? fromStation = null,
            string? toStation = null,
            string? carrierCode = null,
            string? batchId = null,
            string? optionCode = null,
            string? traceId = null) : base(id)
        {
            TaskCode = Check.NotNullOrWhiteSpace(taskCode, nameof(taskCode), maxLength: 64);
            FromStation = fromStation;
            ToStation = toStation;
            CarrierCode = carrierCode;
            BatchId = batchId;
            OptionCode = optionCode;
            TraceId = traceId;
            Status = AgvTaskStatus.Pending;
            StepIndex = 0;
        }

        /// <summary>
        /// 开始执行任务，绑定指派车辆并将状态切换为 Running
        /// </summary>
        /// <param name="vehicleId">AGV 主键</param>
        /// <param name="vehicleCode">AGV 编号</param>
        /// <param name="traceId">贯穿 TraceId</param>
        public void Start(Guid vehicleId, string vehicleCode, string? traceId = null)
        {
            if (Status != AgvTaskStatus.Pending)
            {
                throw new BusinessException("RCS:TaskCannotStart")
                    .WithData("TaskCode", TaskCode)
                    .WithData("CurrentStatus", Status.ToString());
            }

            AssignedVehicleId = vehicleId;
            AssignedVehicleCode = Check.NotNullOrWhiteSpace(vehicleCode, nameof(vehicleCode));
            if (!string.IsNullOrWhiteSpace(traceId))
            {
                TraceId = traceId;
            }

            Status = AgvTaskStatus.Running;
            StartTime = DateTime.UtcNow;
            StepIndex = 1;
        }

        /// <summary>
        /// 步进推进工作流（轻量细粒度步进引擎驱动）
        /// </summary>
        /// <param name="stepIndex">新步进索引</param>
        /// <param name="activeLeg">当前激活程段</param>
        /// <param name="waitingEvent">等待事件标识</param>
        public void AdvanceStep(int stepIndex, string? activeLeg = null, string? waitingEvent = null)
        {
            if (Status != AgvTaskStatus.Running)
            {
                throw new BusinessException("RCS:TaskNotInRunningState")
                    .WithData("TaskCode", TaskCode)
                    .WithData("CurrentStatus", Status.ToString());
            }

            StepIndex = stepIndex;
            ActiveLeg = activeLeg ?? ActiveLeg;
            WaitingEvent = waitingEvent;
        }

        /// <summary>
        /// 调度员重新指派车辆
        /// </summary>
        /// <param name="vehicleId">新指派车辆主键</param>
        /// <param name="vehicleCode">新指派车辆编号</param>
        /// <param name="reason">指派原因说明</param>
        public void AssignVehicle(Guid vehicleId, string vehicleCode, string? reason = null)
        {
            if (Status == AgvTaskStatus.Succeeded || Status == AgvTaskStatus.Canceled || Status == AgvTaskStatus.Failed)
            {
                throw new BusinessException("RCS:TaskAlreadyEnded")
                    .WithData("TaskCode", TaskCode)
                    .WithData("CurrentStatus", Status.ToString());
            }

            AssignedVehicleId = vehicleId;
            AssignedVehicleCode = Check.NotNullOrWhiteSpace(vehicleCode, nameof(vehicleCode));
            if (!string.IsNullOrWhiteSpace(reason))
            {
                FailureReason = reason;
            }
        }

        /// <summary>
        /// 任务正常执行完成，标记状态为 Succeeded
        /// </summary>
        public void Complete()
        {
            if (Status != AgvTaskStatus.Running)
            {
                throw new BusinessException("RCS:TaskCannotComplete")
                    .WithData("TaskCode", TaskCode)
                    .WithData("CurrentStatus", Status.ToString());
            }

            Status = AgvTaskStatus.Succeeded;
            EndTime = DateTime.UtcNow;
            WaitingEvent = null;
        }

        /// <summary>
        /// 任务执行异常失败，标记状态为 Failed
        /// </summary>
        /// <param name="reason">失败根因描述</param>
        public void Fail(string reason)
        {
            Status = AgvTaskStatus.Failed;
            FailureReason = Check.NotNullOrWhiteSpace(reason, nameof(reason));
            EndTime = DateTime.UtcNow;
            WaitingEvent = null;
        }

        /// <summary>
        /// 调度员人工取消任务，标记状态为 Canceled
        /// </summary>
        /// <param name="reason">取消原因说明</param>
        public void Cancel(string reason)
        {
            if (Status == AgvTaskStatus.Succeeded)
            {
                throw new BusinessException("RCS:CannotCancelSucceededTask")
                    .WithData("TaskCode", TaskCode);
            }

            Status = AgvTaskStatus.Canceled;
            FailureReason = Check.NotNullOrWhiteSpace(reason, nameof(reason));
            EndTime = DateTime.UtcNow;
            WaitingEvent = null;
        }

        /// <summary>
        /// 调度员人工强制完结任务（直接置为 Succeeded）
        /// </summary>
        /// <param name="reason">强制完结原因</param>
        public void ForceEnd(string reason)
        {
            Status = AgvTaskStatus.Succeeded;
            FailureReason = $"强制完结: {reason}";
            EndTime = DateTime.UtcNow;
            WaitingEvent = null;
        }
    }
}
