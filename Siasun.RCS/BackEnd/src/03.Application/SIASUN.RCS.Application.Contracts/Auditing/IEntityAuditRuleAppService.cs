using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace SIASUN.RCS.Auditing
{
    public interface IEntityAuditRuleAppService : ICrudAppService<
        EntityAuditRuleDto,
        Guid,
        Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto,
        CreateUpdateEntityAuditRuleDto,
        CreateUpdateEntityAuditRuleDto>
    {
        Task ToggleAsync(Guid id);
        Task<List<EntityTypeDiscoveryDto>> GetDiscoverableEntityTypesAsync();
    }
}
