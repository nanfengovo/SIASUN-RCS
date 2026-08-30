using System;
using System.Collections.Generic;

namespace SIASUN.RCS.Auditing
{
    public class EntityAuditLogMessage
    {
        public string TraceId { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime CreationTime { get; set; }

        // 当模式为 Summary 时，仅存属性名；当为 Full 时，存属性名和新旧值对象字典
        public List<string>? ChangedProperties { get; set; }
        public Dictionary<string, object?>? OriginalValues { get; set; }
        public Dictionary<string, object?>? CurrentValues { get; set; }
    }
}
