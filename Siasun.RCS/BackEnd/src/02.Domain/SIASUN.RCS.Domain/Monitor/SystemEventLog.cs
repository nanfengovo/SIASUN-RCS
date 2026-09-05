using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace SIASUN.RCS.Monitor
{
    /// <summary>
    /// 系统维护与自愈动作追溯日志
    /// </summary>
    public class SystemEventLog : CreationAuditedEntity<Guid>
    {
        /// <summary>
        /// 事件类别（如: DiskSelfHeal, MemoryWarn, CpuWarn）
        /// </summary>
        public string EventCategory { get; set; } = string.Empty;

        /// <summary>
        /// 严重级别（如: Info, Warning, Critical）
        /// </summary>
        public string Level { get; set; } = string.Empty;

        /// <summary>
        /// 事件概览简述
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 事件详细执行结果（可存 JSON）
        /// </summary>
        public string ActionDetails { get; set; } = string.Empty;

        protected SystemEventLog()
        {
        }

        public SystemEventLog(Guid id, string eventCategory, string level, string message, string actionDetails, DateTime? creationTime = null)
            : base(id)
        {
            EventCategory = eventCategory;
            Level = level;
            Message = message;
            ActionDetails = actionDetails;
            if (creationTime.HasValue)
            {
                CreationTime = creationTime.Value;
            }
        }
    }
}

