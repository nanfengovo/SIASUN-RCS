using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SIASUN.RCS.Auditing
{
    public interface IAuditLogFilterRuleAppService : IApplicationService
    {
        Task<PagedResultDto<AuditLogFilterRuleDto>> GetListAsync(GetAuditLogFilterRulesInput input);

        Task<AuditLogFilterRuleDto> GetAsync(Guid id);

        Task<AuditLogFilterRuleDto> CreateAsync(CreateAuditLogFilterRuleDto input);

        Task<AuditLogFilterRuleDto> UpdateAsync(Guid id, UpdateAuditLogFilterRuleDto input);

        Task DeleteAsync(Guid id);

        Task<AuditLogFilterRuleDto> ToggleAsync(Guid id);
    }
}
