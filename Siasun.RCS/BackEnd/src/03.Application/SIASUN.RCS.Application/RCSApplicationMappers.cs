using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;
using SIASUN.RCS.Auditing;

namespace SIASUN.RCS;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class AuditLogFilterRuleMapper : MapperBase<AuditLogFilterRule, AuditLogFilterRuleDto>
{
    public override partial AuditLogFilterRuleDto Map(AuditLogFilterRule source);
    public override partial void Map(AuditLogFilterRule source, AuditLogFilterRuleDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class EntityAuditRuleMapper : MapperBase<EntityAuditRule, EntityAuditRuleDto>
{
    public override partial EntityAuditRuleDto Map(EntityAuditRule source);
    public override partial void Map(EntityAuditRule source, EntityAuditRuleDto destination);
}
