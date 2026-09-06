using System;

namespace SIASUN.RCS.Auditing
{
    /// <summary>
    /// 实体变更审计日志存储实体
    /// 记录领域实体字段变更历史、TraceId 溯源与序列化差异快照
    /// </summary>
    public class EntityAuditLogEntry
    {
        /// <summary>
        /// 自增主键标识
        /// </summary>
        public long Id { get; set; }

        // 跨服务/请求追踪 ID
        /// <summary>
        /// 跨服务/请求追踪唯一标识 (TraceId / CorrelationId)
        /// </summary>
        public string TraceId { get; set; } = string.Empty;

        // 实体名称
        /// <summary>
        /// 变更领域实体的类名或短名称
        /// </summary>
        public string EntityName { get; set; } = string.Empty;

        // 实体主键
        /// <summary>
        /// 变更领域实体的主键字符串标识
        /// </summary>
        public string EntityId { get; set; } = string.Empty;

        // 动作类型
        /// <summary>
        /// 变更动作类型（Added, Modified, Deleted 等）
        /// </summary>
        public string Action { get; set; } = string.Empty;

        // 序列化的属性变更（新旧值）
        /// <summary>
        /// 序列化的属性变更详情快照 JSON（包含前后值差异）
        /// </summary>
        public string PropertyChangesJson { get; set; } = string.Empty;

        /// <summary>
        /// 变更发生的时间戳 (UTC)
        /// </summary>
        public DateTime CreationTime { get; set; } = DateTime.UtcNow;
    }
}
