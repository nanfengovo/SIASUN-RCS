using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SIASUN.RCS.Permissions;
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

            if (!string.IsNullOrWhiteSpace(input.Filter))
            {
                var filter = input.Filter.Trim();
                query = query.Where(x => x.Name.Contains(filter) || x.PathPattern.Contains(filter) || (x.Description != null && x.Description.Contains(filter)));
            }

            if (input.RuleType.HasValue)
            {
                query = query.Where(x => x.RuleType == input.RuleType.Value);
            }

            if (input.Direction.HasValue)
            {
                query = query.Where(x => x.Direction == input.Direction.Value);
            }

            if (input.IsEnabled.HasValue)
            {
                query = query.Where(x => x.IsEnabled == input.IsEnabled.Value);
            }

            var totalCount = await AsyncExecuter.CountAsync(query);

            var items = await AsyncExecuter.ToListAsync(
                query.OrderByDescending(x => x.CreationTime)
                     .Skip(input.SkipCount)
                     .Take(input.MaxResultCount)
            );

            var dtos = items.Select(x => MapToDto(x)).ToList();

            return new PagedResultDto<AuditLogFilterRuleDto>(totalCount, dtos);
        }

        public async Task<AuditLogFilterRuleDto> GetAsync(Guid id)
        {
            var entity = await _ruleRepository.GetAsync(id);
            return MapToDto(entity);
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

            return MapToDto(entity);
        }

        [Authorize(RCSPermissions.AuditLogFilterRules.Edit)]
        public async Task<AuditLogFilterRuleDto> UpdateAsync(Guid id, UpdateAuditLogFilterRuleDto input)
        {
            var entity = await _ruleRepository.GetAsync(id);
            entity.Name = input.Name;
            entity.PathPattern = input.PathPattern;
            entity.RuleType = input.RuleType;
            entity.Direction = input.Direction;
            entity.HttpMethod = input.HttpMethod;
            entity.IsEnabled = input.IsEnabled;
            entity.Description = input.Description;

            await _ruleRepository.UpdateAsync(entity, autoSave: true);

            await _localEventBus.PublishAsync(new AuditFilterRulesChangedEvent());

            return MapToDto(entity);
        }

        [Authorize(RCSPermissions.AuditLogFilterRules.Delete)]
        public async Task DeleteAsync(Guid id)
        {
            await _ruleRepository.DeleteAsync(id, autoSave: true);

            await _localEventBus.PublishAsync(new AuditFilterRulesChangedEvent());
        }

        [Authorize(RCSPermissions.AuditLogFilterRules.Edit)]
        public async Task<AuditLogFilterRuleDto> ToggleAsync(Guid id)
        {
            var entity = await _ruleRepository.GetAsync(id);
            entity.IsEnabled = !entity.IsEnabled;
            await _ruleRepository.UpdateAsync(entity, autoSave: true);

            await _localEventBus.PublishAsync(new AuditFilterRulesChangedEvent());

            return MapToDto(entity);
        }

        private static AuditLogFilterRuleDto MapToDto(AuditLogFilterRule entity)
        {
            return new AuditLogFilterRuleDto
            {
                Id = entity.Id,
                Name = entity.Name,
                PathPattern = entity.PathPattern,
                RuleType = entity.RuleType,
                Direction = entity.Direction,
                HttpMethod = entity.HttpMethod,
                IsEnabled = entity.IsEnabled,
                Description = entity.Description,
                CreationTime = entity.CreationTime,
                CreatorId = entity.CreatorId,
                LastModificationTime = entity.LastModificationTime,
                LastModifierId = entity.LastModifierId,
                ConcurrencyStamp = entity.ConcurrencyStamp
            };
        }
    }
}
