using System;

namespace SIASUN.RCS.Auditing
{
    public class EntityAuditLogEntry
    {
        public long Id { get; set; }
        
        // 跨服务/请求追踪 ID
        public string TraceId { get; set; } = string.Empty;

        // 实体名称
        public string EntityName { get; set; } = string.Empty;
        
        // 实体主键
        public string EntityId { get; set; } = string.Empty;
        
        // 动作类型
        public string Action { get; set; } = string.Empty;
        
        // 序列化的属性变更（新旧值）
        public string PropertyChangesJson { get; set; } = string.Empty;

        public DateTime CreationTime { get; set; } = DateTime.UtcNow;
    }
}
