using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SIASUN.RCS.Permissions;
using System.Linq;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;

namespace SIASUN.RCS.Auditing
{
    [Authorize(RCSPermissions.AuditLogFilterRules.Default)]
    public class AuditLogFilterRuleAppService : ApplicationService, IAuditLogFilterRuleAppService
    {
        private readonly IRepository<AuditLogFilterRule, Guid> _ruleRepository;
        private readonly ILocalEventBus _localEventBus;

        public AuditLogFilterRuleAppService(
            IRepository<AuditLogFilterRule, Guid> ruleRepository,
            ILocalEventBus localEventBus)
        {
            _ruleRepository = ruleRepository;
            _localEventBus = localEventBus;
        }

        public async Task<PagedResultDto<AuditLogFilterRuleDto>> GetListAsync(GetAuditLogFilterRulesInput input)
        {
            var query = await _ruleRepository.GetQueryableAsync();
            if (!string.IsNullOrWhiteSpace(input.Filter)) query = query.Where(x => x.Name.Contains(input.Filter) || x.PathPattern.Contains(input.Filter));
            var count = await AsyncExecuter.CountAsync(query);
            var list = await AsyncExecuter.ToListAsync(query.OrderByDescending(x => x.CreationTime).Skip(input.SkipCount).Take(input.MaxResultCount));
            
            return new PagedResultDto<AuditLogFilterRuleDto>(count, ObjectMapper.Map<List<AuditLogFilterRule>, List<AuditLogFilterRuleDto>>(list));
        }

        public async Task<AuditLogFilterRuleDto> GetAsync(Guid id)
        {
            var entity = await _ruleRepository.GetAsync(id);
            return ObjectMapper.Map<AuditLogFilterRule, AuditLogFilterRuleDto>(entity);
        }

        [Authorize(RCSPermissions.AuditLogFilterRules.Create)]
        public async Task<AuditLogFilterRuleDto> CreateAsync(CreateAuditLogFilterRuleDto input)
        {
            var entity = new AuditLogFilterRule(
                GuidGenerator.Create(),
                input.Name,
                input.PathPattern,
                input.RuleType,
                input.Direction,
                input.HttpMethod,
                input.IsEnabled,
                input.Description
            );
            
            await _ruleRepository.InsertAsync(entity, autoSave: true);
            await _localEventBus.PublishAsync(new AuditFilterRulesChangedEvent());

            return ObjectMapper.Map<AuditLogFilterRule, AuditLogFilterRuleDto>(entity);
        }

        [Authorize(RCSPermissions.AuditLogFilterRules.Edit)]
        public async Task<AuditLogFilterRuleDto> UpdateAsync(Guid id, UpdateAuditLogFilterRuleDto input)
        {
            var entity = await _ruleRepository.GetAsync(id);
            
            entity.Update(input.Name, input.PathPattern, input.RuleType, input.Direction, input.HttpMethod, input.IsEnabled, input.Description);

            await _ruleRepository.UpdateAsync(entity, autoSave: true);
            await _localEventBus.PublishAsync(new AuditFilterRulesChangedEvent());

            return ObjectMapper.Map<AuditLogFilterRule, AuditLogFilterRuleDto>(entity);
        }

        [Authorize(RCSPermissions.AuditLogFilterRules.Edit)]
        public async Task<AuditLogFilterRuleDto> ToggleAsync(Guid id)
        {
            var entity = await _ruleRepository.GetAsync(id);
            entity.Toggle();
            await _ruleRepository.UpdateAsync(entity, autoSave: true);
            await _localEventBus.PublishAsync(new AuditFilterRulesChangedEvent());

            return ObjectMapper.Map<AuditLogFilterRule, AuditLogFilterRuleDto>(entity);
        }

        [Authorize(RCSPermissions.AuditLogFilterRules.Delete)]
        public async Task DeleteAsync(Guid id)
        {
            await _ruleRepository.DeleteAsync(id, autoSave: true);
            await _localEventBus.PublishAsync(new AuditFilterRulesChangedEvent());
        }
    }
}
