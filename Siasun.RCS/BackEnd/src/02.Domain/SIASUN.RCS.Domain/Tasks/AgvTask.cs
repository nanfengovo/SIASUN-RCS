using System;
using SIASUN.RCS.Tasks.Events;
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
        /// 编译生成 OptionCode 所使用的 Schema 代号（例如 "txc_demo"、"erack"、"molding"）
        /// </summary>
        public string? OptionCodeSchemaCode { get; private set; }

        /// <summary>
        /// 编译生成 OptionCode 所使用的 Schema 版本号
        /// </summary>
        public int? OptionCodeSchemaVersion { get; private set; }

        /// <summary>
        /// 绑定的声明式工作流定义编号（例如 "transfer_standard", "erack_docking"）
        /// </summary>
        public string? WorkflowDefinitionId { get; private set; }

        /// <summary>
        /// 工作流定义标识代号（兼容契约别名）
        /// </summary>
        public string? WorkflowKey => WorkflowDefinitionId;

        /// <summary>
        /// 绑定的声明式工作流版本号
        /// </summary>
        public int? WorkflowVersion { get; private set; }

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
        /// 当前步骤重试次数（仅在可幂等步进中由 Polly 自动重试推进）
        /// </summary>
        public int RetryCount { get; private set; }

        /// <summary>
        /// 最大允许重试次数（默认为 3 次）
        /// </summary>
        public int MaxRetryCount { get; private set; } = 3;

        /// <summary>
        /// 最近一次重试发生时间（UTC）
        /// </summary>
        public DateTime? LastRetryTime { get; private set; }

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
        /// <param name="optionCodeSchemaCode">使用的 Schema 代号</param>
        /// <param name="optionCodeSchemaVersion">使用的 Schema 版本号</param>
        public AgvTask(
            Guid id,
            string taskCode,
            string? fromStation = null,
            string? toStation = null,
            string? carrierCode = null,
            string? batchId = null,
            string? optionCode = null,
            string? traceId = null,
            string? optionCodeSchemaCode = null,
            int? optionCodeSchemaVersion = null) : base(id)
        {
            TaskCode = Check.NotNullOrWhiteSpace(taskCode, nameof(taskCode), maxLength: 64);
            FromStation = fromStation;
            ToStation = toStation;
            CarrierCode = carrierCode;
            BatchId = batchId;
            OptionCode = optionCode;
            TraceId = traceId;
            OptionCodeSchemaCode = optionCodeSchemaCode;
            OptionCodeSchemaVersion = optionCodeSchemaVersion;
            Status = AgvTaskStatus.Pending;
            StepIndex = 0;
        }

        /// <summary>
        /// 开始执行任务，绑定指派车辆并将状态切换为 Running
        /// </summary>
        /// <param name="vehicleId">AGV 主键</param>
        /// <param name="vehicleCode">AGV 编号</param>
        /// <param name="traceId">贯穿 TraceId</param>
        /// <param name="startTime">任务开始时间（可选，默认当前时间）</param>
        public void Start(Guid vehicleId, string vehicleCode, string? traceId = null, DateTime? startTime = null)
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
            StartTime = startTime ?? DateTime.UtcNow;
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
            RetryCount = 0; // 成功推进到下一步时重置当前步重试计数
        }

        /// <summary>
        /// 挂起当前任务步进，进入等待外部事件信号状态（如等待 TM 动作完成、PLC 门开启）
        /// </summary>
        /// <param name="waitingEvent">等待事件标识</param>
        public void Suspend(string waitingEvent)
        {
            if (Status != AgvTaskStatus.Running)
            {
                throw new BusinessException("RCS:TaskNotInRunningState")
                    .WithData("TaskCode", TaskCode)
                    .WithData("CurrentStatus", Status.ToString());
            }

            WaitingEvent = Check.NotNullOrWhiteSpace(waitingEvent, nameof(waitingEvent));
            AddLocalEvent(new TaskStepSuspendedEvent(Id, TaskCode, StepIndex, ActiveLeg, WaitingEvent, TraceId));
        }

        /// <summary>
        /// 收到外部异步信号，唤醒挂起中的任务步进
        /// </summary>
        /// <param name="receivedEvent">收到的外部事件标识</param>
        public void ResumeByEvent(string receivedEvent)
        {
            if (Status != AgvTaskStatus.Running)
            {
                throw new BusinessException("RCS:TaskNotInRunningState")
                    .WithData("TaskCode", TaskCode)
                    .WithData("CurrentStatus", Status.ToString());
            }

            if (!string.Equals(WaitingEvent, receivedEvent, StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessException("RCS:WaitingEventMismatch")
                    .WithData("TaskCode", TaskCode)
                    .WithData("ExpectedEvent", WaitingEvent ?? string.Empty)
                    .WithData("ReceivedEvent", receivedEvent);
            }

            WaitingEvent = null;
            AddLocalEvent(new TaskStepResumedEvent(Id, TaskCode, StepIndex, receivedEvent, TraceId));
        }

        /// <summary>
        /// 记录当前步骤的瞬态重试（仅允许在可幂等步进中重试，且不能突破最大重试上限）
        /// </summary>
        /// <param name="stepName">步骤名称</param>
        /// <param name="errorMessage">瞬态异常消息</param>
        /// <param name="isIdempotent">该步骤是否为安全可幂等操作</param>
        public void RecordRetry(string stepName, string errorMessage, bool isIdempotent)
        {
            if (Status != AgvTaskStatus.Running)
            {
                throw new BusinessException("RCS:TaskNotInRunningState")
                    .WithData("TaskCode", TaskCode)
                    .WithData("CurrentStatus", Status.ToString());
            }

            if (!isIdempotent)
            {
                // 非幂等动作（如举升取货）严禁盲重试，必须直接转入 Failed 等待人工干预或 SAGA 补偿
                Fail($"非幂等步骤 [{stepName}] 发生异常，禁止自动重试: {errorMessage}");
                return;
            }

            RetryCount++;
            LastRetryTime = DateTime.UtcNow;

            AddLocalEvent(new TaskStepRetriedEvent(
                Id,
                TaskCode,
                StepIndex,
                stepName,
                RetryCount,
                MaxRetryCount,
                isIdempotent,
                errorMessage,
                TraceId,
                LastRetryTime.Value));

            if (RetryCount > MaxRetryCount)
            {
                Fail($"可幂等步骤 [{stepName}] 重试达到上限 ({MaxRetryCount} 次)，最终失败: {errorMessage}");
            }
        }

        /// <summary>
        /// 从 Failed 状态显式恢复至 Running（显式领域方法，严禁外部直接篡改状态属性）
        /// </summary>
        /// <param name="reason">调度员人工重试原因</param>
        /// <param name="retryCurrentStep">是否重试当前失败步骤（true: 保持当前 StepIndex；false: 跳过当前步骤推进至下一步）</param>
        public void ResumeFromFailure(string reason, bool retryCurrentStep = true)
        {
            if (Status != AgvTaskStatus.Failed)
            {
                throw new BusinessException("RCS:TaskNotFailed")
                    .WithData("TaskCode", TaskCode)
                    .WithData("CurrentStatus", Status.ToString());
            }

            Status = AgvTaskStatus.Running;
            FailureReason = null;
            EndTime = null;
            RetryCount = 0;

            if (!retryCurrentStep)
            {
                StepIndex++;
            }

            AddLocalEvent(new TaskLifecycleResumedEvent(Id, TaskCode, Check.NotNullOrWhiteSpace(reason, nameof(reason)), StepIndex, TraceId));
        }

        /// <summary>
        /// 执行 SAGA 补偿回退（步退至历史安全步骤，例如取货失败回退至对位点）
        /// </summary>
        /// <param name="targetStepIndex">回退的目标步骤序号（0 至当前步骤索引）</param>
        /// <param name="reason">补偿回退原因</param>
        public void RollbackToStep(int targetStepIndex, string reason)
        {
            if (targetStepIndex < 0 || targetStepIndex > StepIndex)
            {
                throw new BusinessException("RCS:InvalidRollbackStepIndex")
                    .WithData("TaskCode", TaskCode)
                    .WithData("CurrentStepIndex", StepIndex)
                    .WithData("TargetStepIndex", targetStepIndex);
            }

            var fromStep = StepIndex;
            StepIndex = targetStepIndex;
            Status = AgvTaskStatus.Running;
            WaitingEvent = null;
            FailureReason = null;
            EndTime = null;
            RetryCount = 0;

            AddLocalEvent(new TaskStepCompensatedEvent(Id, TaskCode, fromStep, targetStepIndex, Check.NotNullOrWhiteSpace(reason, nameof(reason)), TraceId));
        }

        /// <summary>
        /// 配置任务允许的最大重试次数
        /// </summary>
        /// <param name="maxRetryCount">最大重试次数</param>
        public void ConfigureMaxRetryCount(int maxRetryCount)
        {
            MaxRetryCount = Math.Max(0, maxRetryCount);
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
        }

        /// <summary>
        /// 任务正常执行完成，标记状态为 Succeeded
        /// </summary>
        /// <param name="endTime">任务完成时间（可选，默认当前时间）</param>
        public void Complete(DateTime? endTime = null)
        {
            if (Status != AgvTaskStatus.Running)
            {
                throw new BusinessException("RCS:TaskCannotComplete")
                    .WithData("TaskCode", TaskCode)
                    .WithData("CurrentStatus", Status.ToString());
            }

            Status = AgvTaskStatus.Succeeded;
            EndTime = endTime ?? DateTime.UtcNow;
            WaitingEvent = null;
            RetryCount = 0;

            AddLocalEvent(new TaskLifecycleEndedEvent(
                Id,
                TaskCode,
                Status,
                null,
                TraceId,
                AssignedVehicleCode,
                EndTime.Value));
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

            AddLocalEvent(new TaskLifecycleEndedEvent(
                Id,
                TaskCode,
                Status,
                FailureReason,
                TraceId,
                AssignedVehicleCode,
                EndTime.Value));
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

            AddLocalEvent(new TaskLifecycleEndedEvent(
                Id,
                TaskCode,
                Status,
                FailureReason,
                TraceId,
                AssignedVehicleCode,
                EndTime.Value));
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

            AddLocalEvent(new TaskLifecycleEndedEvent(
                Id,
                TaskCode,
                Status,
                FailureReason,
                TraceId,
                AssignedVehicleCode,
                EndTime.Value));
        }

        /// <summary>
        /// 冻结并固化任务 OptionCode 及其关联 Schema 快照（杜绝主数据修改导致在途运单漂移）
        /// </summary>
        /// <param name="optionCode">编译好的 OptionCode 报文字符串</param>
        /// <param name="schemaCode">使用的 Schema 代号（可选）</param>
        /// <param name="schemaVersion">使用的 Schema 版本号（可选）</param>
        public void FreezeOptionCode(string optionCode, string? schemaCode = null, int? schemaVersion = null)
        {
            OptionCode = Check.NotNullOrWhiteSpace(optionCode, nameof(optionCode));
            if (!string.IsNullOrWhiteSpace(schemaCode))
            {
                OptionCodeSchemaCode = schemaCode;
            }
            if (schemaVersion.HasValue)
            {
                OptionCodeSchemaVersion = schemaVersion;
            }
        }

        /// <summary>
        /// 绑定该任务执行所依赖的声明式工作流 Schema 编号与版本
        /// </summary>
        /// <param name="workflowDefinitionId">工作流代号（例如 "transfer_standard", "erack_docking"）</param>
        /// <param name="version">指定版本号，若为空则默认为模板最新版本</param>
        public void BindWorkflow(string workflowDefinitionId, int? version = null)
        {
            WorkflowDefinitionId = Check.NotNullOrWhiteSpace(workflowDefinitionId, nameof(workflowDefinitionId));
            WorkflowVersion = version;
        }

        /// <summary>
        /// 设置或绑定工作流定义标识键（兼容契约别名）
        /// </summary>
        /// <param name="workflowKey">工作流定义代号</param>
        /// <param name="version">可选指定版本号</param>
        public void SetWorkflowKey(string workflowKey, int? version = null)
        {
            BindWorkflow(workflowKey, version);
        }
    }
}
