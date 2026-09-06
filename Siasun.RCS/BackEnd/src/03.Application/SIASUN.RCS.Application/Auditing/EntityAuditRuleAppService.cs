using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;
using SIASUN.RCS.Logs.OperatorLogs;
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
        private readonly IEntityTypeProvider _entityTypeProvider;
        private readonly SIASUN.RCS.Interfaces.OperationLogs.IOperationLogRecorder _operationLogRecorder;

        public EntityAuditRuleAppService(
            IRepository<EntityAuditRule, Guid> repository,
            ILocalEventBus localEventBus,
            IEntityTypeProvider entityTypeProvider,
            SIASUN.RCS.Interfaces.OperationLogs.IOperationLogRecorder operationLogRecorder) : base(repository)
        {
            _localEventBus = localEventBus;
            _entityTypeProvider = entityTypeProvider;
            _operationLogRecorder = operationLogRecorder;

            CreatePolicyName = RCSPermissions.EntityAuditRules.Create;
            UpdatePolicyName = RCSPermissions.EntityAuditRules.Edit;
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

        /// <summary>
        /// 切换指定实体审计规则的启用/禁用状态
        /// </summary>
        /// <param name="id">规则唯一标识</param>
        [Authorize(RCSPermissions.EntityAuditRules.Edit)]
        [OperationLog(Module = "Auditing", Action = "ToggleEntityRule", TargetType = "EntityAuditRule", Description = "切换实体审计规则启用状态")]
        public async Task ToggleAsync(Guid id)
        {
            var entity = await Repository.GetAsync(id);
            var beforeState = entity.IsEnabled ? "Enabled" : "Disabled";
            entity.Toggle();
            await Repository.UpdateAsync(entity);
            if (CurrentUnitOfWork != null)
            {
                await CurrentUnitOfWork.SaveChangesAsync();
            }
            await _localEventBus.PublishAsync(new EntityAuditRulesChangedEvent());

            var afterState = entity.IsEnabled ? "Enabled" : "Disabled";
            _operationLogRecorder.Record(new SIASUN.RCS.Logs.OperatorLogs.OperationLogContext
            {
                Module = "Auditing",
                Action = "ToggleEntityRule",
                TargetType = "EntityAuditRule",
                TargetId = id.ToString(),
                BeforeState = beforeState,
                AfterState = afterState,
                Description = $"切换实体审计规则 [{entity.Name}] 状态为 [{afterState}]",
                Reason = $"运维人员切换实体审计规则 [{entity.Name}] 启用状态"
            });
        }

        /// <summary>
        /// 创建一条新的实体变更审计规则
        /// </summary>
        /// <param name="input">创建规则入参</param>
        /// <returns>创建成功的规则 DTO</returns>
        [OperationLog(Module = "Auditing", Action = "CreateEntityRule", TargetType = "EntityAuditRule", Description = "创建实体变更审计规则")]
        public override async Task<EntityAuditRuleDto> CreateAsync(CreateUpdateEntityAuditRuleDto input)
        {
            var result = await base.CreateAsync(input);
            await _localEventBus.PublishAsync(new EntityAuditRulesChangedEvent());

            _operationLogRecorder.Record(new SIASUN.RCS.Logs.OperatorLogs.OperationLogContext
            {
                Module = "Auditing",
                Action = "CreateEntityRule",
                TargetType = "EntityAuditRule",
                TargetId = result.Id.ToString(),
                BeforeState = null,
                AfterState = result.IsEnabled ? "Enabled" : "Disabled",
                Description = $"创建实体审计规则 [{result.Name}]",
                Reason = $"运维人员新建实体审计规则 [{result.Name}], 模式: {result.Mode}, 实体模式: {result.EntityTypePattern}"
            });

            return result;
        }

        /// <summary>
        /// 更新指定的实体变更审计规则
        /// </summary>
        /// <param name="id">规则唯一标识</param>
        /// <param name="input">更新参数</param>
        /// <returns>更新后的规则 DTO</returns>
        [OperationLog(Module = "Auditing", Action = "UpdateEntityRule", TargetType = "EntityAuditRule", Description = "更新实体变更审计规则")]
        public override async Task<EntityAuditRuleDto> UpdateAsync(Guid id, CreateUpdateEntityAuditRuleDto input)
        {
            var result = await base.UpdateAsync(id, input);
            await _localEventBus.PublishAsync(new EntityAuditRulesChangedEvent());

            _operationLogRecorder.Record(new SIASUN.RCS.Logs.OperatorLogs.OperationLogContext
            {
                Module = "Auditing",
                Action = "UpdateEntityRule",
                TargetType = "EntityAuditRule",
                TargetId = id.ToString(),
                BeforeState = null,
                AfterState = $"Enabled={result.IsEnabled},Mode={result.Mode},Pattern={result.EntityTypePattern}",
                Description = $"更新实体审计规则 [{result.Name}]",
                Reason = $"运维人员修改实体审计规则 [{result.Name}] 配置"
            });

            return result;
        }

        /// <summary>
        /// 删除指定的实体变更审计规则
        /// </summary>
        /// <param name="id">规则唯一标识</param>
        [OperationLog(Module = "Auditing", Action = "DeleteEntityRule", TargetType = "EntityAuditRule", Description = "删除实体变更审计规则")]
        public override async Task DeleteAsync(Guid id)
        {
            await base.DeleteAsync(id);
            await _localEventBus.PublishAsync(new EntityAuditRulesChangedEvent());

            _operationLogRecorder.Record(new SIASUN.RCS.Logs.OperatorLogs.OperationLogContext
            {
                Module = "Auditing",
                Action = "DeleteEntityRule",
                TargetType = "EntityAuditRule",
                TargetId = id.ToString(),
                BeforeState = "Existing",
                AfterState = "Deleted",
                Description = $"删除实体审计规则 [{id}]",
                Reason = $"运维人员删除实体审计规则 [{id}]"
            });
        }

        /// <summary>
        /// 获取系统中所有可发现的领域实体类型列表及其是否已配置审计规则
        /// </summary>
        /// <returns>可发现实体列表</returns>
        public async Task<List<EntityTypeDiscoveryDto>> GetDiscoverableEntityTypesAsync()
        {
            var entityTypes = _entityTypeProvider.GetEntityTypes();

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
