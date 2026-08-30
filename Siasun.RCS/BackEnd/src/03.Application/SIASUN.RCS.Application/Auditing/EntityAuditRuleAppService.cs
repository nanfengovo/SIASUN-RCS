using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;
using SIASUN.RCS.Permissions;

namespace SIASUN.RCS.Auditing
{
    [Authorize(RCSPermissions.EntityAuditRules.Default)]
    public class EntityAuditRuleAppService : 
        CrudAppService<
            EntityAuditRule, 
            EntityAuditRuleDto, 
            Guid, 
            Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto, 
            CreateUpdateEntityAuditRuleDto, 
            CreateUpdateEntityAuditRuleDto>,
        IEntityAuditRuleAppService
    {
        private readonly ILocalEventBus _localEventBus;

        public EntityAuditRuleAppService(
            IRepository<EntityAuditRule, Guid> repository,
            ILocalEventBus localEventBus) : base(repository)
        {
            _localEventBus = localEventBus;
            
            CreatePolicyName = RCSPermissions.EntityAuditRules.Create;
            UpdatePolicyName = RCSPermissions.EntityAuditRules.Update;
            DeletePolicyName = RCSPermissions.EntityAuditRules.Delete;
        }

        protected override async Task<EntityAuditRule> MapToEntityAsync(CreateUpdateEntityAuditRuleDto createInput)
        {
            var entity = new EntityAuditRule(
                GuidGenerator.Create(),
                createInput.Name,
                createInput.EntityTypePattern,
                createInput.Mode,
                createInput.SampleIntervalMs,
                createInput.ExcludedProperties,
                createInput.Priority,
                createInput.IsEnabled
            );
            return await Task.FromResult(entity);
        }

        protected override async Task MapToEntityAsync(CreateUpdateEntityAuditRuleDto updateInput, EntityAuditRule entity)
        {
            entity.Update(
                updateInput.Name,
                updateInput.EntityTypePattern,
                updateInput.Mode,
                updateInput.SampleIntervalMs,
                updateInput.ExcludedProperties,
                updateInput.Priority
            );
            
            if (updateInput.IsEnabled && !entity.IsEnabled) entity.Enable();
            if (!updateInput.IsEnabled && entity.IsEnabled) entity.Disable();
            
            await Task.CompletedTask;
        }

        [Authorize(RCSPermissions.EntityAuditRules.Update)]
        public async Task ToggleAsync(Guid id)
        {
            var entity = await Repository.GetAsync(id);
            entity.Toggle();
            await Repository.UpdateAsync(entity);
            await CurrentUnitOfWork.SaveChangesAsync();
            await _localEventBus.PublishAsync(new EntityAuditRulesChangedEvent());
        }

        public override async Task<EntityAuditRuleDto> CreateAsync(CreateUpdateEntityAuditRuleDto input)
        {
            var result = await base.CreateAsync(input);
            await _localEventBus.PublishAsync(new EntityAuditRulesChangedEvent());
            return result;
        }

        public override async Task<EntityAuditRuleDto> UpdateAsync(Guid id, CreateUpdateEntityAuditRuleDto input)
        {
            var result = await base.UpdateAsync(id, input);
            await _localEventBus.PublishAsync(new EntityAuditRulesChangedEvent());
            return result;
        }

        public override async Task DeleteAsync(Guid id)
        {
            await base.DeleteAsync(id);
            await _localEventBus.PublishAsync(new EntityAuditRulesChangedEvent());
        }

        public async Task<List<EntityTypeDiscoveryDto>> GetDiscoverableEntityTypesAsync()
        {
            // 通过反射扫描当前应用程序域中所有继承自 IEntity 的实体类
            var entityTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName != null && a.FullName.Contains("SIASUN.RCS"))
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch
                    {
                        return Array.Empty<Type>();
                    }
                })
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IEntity).IsAssignableFrom(t))
                .ToList();

            var existingRules = await Repository.GetListAsync();
            var existingPatterns = existingRules.Select(r => r.EntityTypePattern).ToHashSet();

            var result = entityTypes.Select(t => new EntityTypeDiscoveryDto
            {
                FullName = t.FullName ?? t.Name,
                ShortName = t.Name,
                HasRule = existingPatterns.Contains(t.FullName ?? t.Name) || existingPatterns.Contains(t.Name)
            })
            .OrderBy(x => x.FullName)
            .ToList();

            return result;
        }
    }
}
