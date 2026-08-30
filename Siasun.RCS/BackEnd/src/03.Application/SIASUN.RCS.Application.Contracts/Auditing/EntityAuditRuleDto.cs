using System;
using Volo.Abp.Application.Dtos;

namespace SIASUN.RCS.Auditing
{
    public class EntityAuditRuleDto : FullAuditedEntityDto<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string EntityTypePattern { get; set; } = string.Empty;
        public EntityAuditMode Mode { get; set; }
        public int SampleIntervalMs { get; set; }
        public string? ExcludedProperties { get; set; }
        public int Priority { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class CreateUpdateEntityAuditRuleDto
    {
        public string Name { get; set; } = string.Empty;
        public string EntityTypePattern { get; set; } = string.Empty;
        public EntityAuditMode Mode { get; set; }
        public int SampleIntervalMs { get; set; }
        public string? ExcludedProperties { get; set; }
        public int Priority { get; set; }
        public bool IsEnabled { get; set; }
    }
}
