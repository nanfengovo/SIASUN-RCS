using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;

namespace SIASUN.RCS.Tasks
{
    /// <summary>
    /// TM 底层序列号与内部调度任务双向持久化映射实体（严格落地选项 B：跨进程崩溃自愈与多程段精确流转）
    /// </summary>
    public class TaskSerialMapping : CreationAuditedEntity<Guid>
    {
        /// <summary>
        /// 内部 AGV 任务实体 ID
        /// </summary>
        public Guid TaskId { get; private set; }

        /// <summary>
        /// 内部 AGV 业务任务编号（例如 "TSK-202609080001"）
        /// </summary>
        public string TaskCode { get; private set; } = string.Empty;

        /// <summary>
        /// 底层 TM 报文中的任务序列号（全局唯一，例如 "TM_20260908_001_FETCH"）
        /// </summary>
        public string TmSerial { get; private set; } = string.Empty;

        /// <summary>
        /// 关联的多程段标识（例如 "Fetch" / "Put" / "Move"）
        /// </summary>
        public string Leg { get; private set; } = string.Empty;

        /// <summary>
        /// 关联的轻量工作流步骤索引（StepIndex）
        /// </summary>
        public int StepIndex { get; private set; }

        /// <summary>
        /// 挂起等待唤醒的领域事件标识（例如 "TM_FETCH_DONE"）
        /// </summary>
        public string? WaitingEvent { get; private set; }

        /// <summary>
        /// 执行该程段的 AGV 编号
        /// </summary>
        public string? VehicleCode { get; private set; }

        /// <summary>
        /// EF Core 反序列化构造函数
        /// </summary>
        protected TaskSerialMapping()
        {
        }

        /// <summary>
        /// 创建新的 TM 序列号映射实体
        /// </summary>
        public TaskSerialMapping(
            Guid id,
            Guid taskId,
            string taskCode,
            string tmSerial,
            string leg,
            int stepIndex,
            string? waitingEvent = null,
            string? vehicleCode = null)
            : base(id)
        {
            TaskId = taskId;
            TaskCode = Check.NotNullOrWhiteSpace(taskCode, nameof(taskCode));
            TmSerial = Check.NotNullOrWhiteSpace(tmSerial, nameof(tmSerial));
            Leg = Check.NotNullOrWhiteSpace(leg, nameof(leg));
            StepIndex = stepIndex;
            WaitingEvent = waitingEvent;
            VehicleCode = vehicleCode;
        }
    }
}
