using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SIASUN.RCS.Logs.OperatorLogs;
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
        private readonly SIASUN.RCS.Interfaces.OperationLogs.IOperationLogRecorder _operationLogRecorder;

        public AuditLogFilterRuleAppService(
            IRepository<AuditLogFilterRule, Guid> ruleRepository,
            ILocalEventBus localEventBus,
            SIASUN.RCS.Interfaces.OperationLogs.IOperationLogRecorder operationLogRecorder)
        {
            _ruleRepository = ruleRepository;
            _localEventBus = localEventBus;
            _operationLogRecorder = operationLogRecorder;
        }

        /// <summary>
        /// 分页查询 API 审计过滤规则
        /// </summary>
        /// <param name="input">查询与分页参数</param>
        /// <returns>分页规则列表</returns>
        public async Task<PagedResultDto<AuditLogFilterRuleDto>> GetListAsync(GetAuditLogFilterRulesInput input)
        {
            var query = await _ruleRepository.GetQueryableAsync();
            if (!string.IsNullOrWhiteSpace(input.Filter)) query = query.Where(x => x.Name.Contains(input.Filter) || x.PathPattern.Contains(input.Filter));
            var count = await AsyncExecuter.CountAsync(query);
            var list = await AsyncExecuter.ToListAsync(query.OrderByDescending(x => x.CreationTime).Skip(input.SkipCount).Take(input.MaxResultCount));
            
            return new PagedResultDto<AuditLogFilterRuleDto>(count, ObjectMapper.Map<List<AuditLogFilterRule>, List<AuditLogFilterRuleDto>>(list));
        }

        /// <summary>
        /// 根据唯一标识获取单个 API 审计过滤规则详情
        /// </summary>
        /// <param name="id">规则唯一标识</param>
        /// <returns>规则详细 DTO</returns>
        public async Task<AuditLogFilterRuleDto> GetAsync(Guid id)
        {
            var entity = await _ruleRepository.GetAsync(id);
            return ObjectMapper.Map<AuditLogFilterRule, AuditLogFilterRuleDto>(entity);
        }

        /// <summary>
        /// 创建一条新的 API 审计过滤规则
        /// </summary>
        /// <param name="input">新建规则参数</param>
        /// <returns>已创建的规则 DTO</returns>
        [Authorize(RCSPermissions.AuditLogFilterRules.Create)]
        [OperationLog(Module = "Auditing", Action = "CreateFilterRule", TargetType = "AuditFilterRule", Description = "创建 API 审计过滤规则")]
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

            _operationLogRecorder.Record(new SIASUN.RCS.Logs.OperatorLogs.OperationLogContext
            {
                Module = "Auditing",
                Action = "CreateFilterRule",
                TargetType = "AuditFilterRule",
                TargetId = entity.Id.ToString(),
                BeforeState = null,
                AfterState = entity.IsEnabled ? "Enabled" : "Disabled",
                Description = $"创建 API 审计过滤规则 [{entity.Name}]",
                Reason = $"运维人员新建 API 审计过滤规则 [{entity.Name}], 模式: {entity.RuleType}, 路径: {entity.PathPattern}"
            });

            return ObjectMapper.Map<AuditLogFilterRule, AuditLogFilterRuleDto>(entity);
        }

        /// <summary>
        /// 更新指定的 API 审计过滤规则
        /// </summary>
        /// <param name="id">规则唯一标识</param>
        /// <param name="input">更新参数</param>
        /// <returns>已更新的规则 DTO</returns>
        [Authorize(RCSPermissions.AuditLogFilterRules.Edit)]
        [OperationLog(Module = "Auditing", Action = "UpdateFilterRule", TargetType = "AuditFilterRule", Description = "更新 API 审计过滤规则")]
        public async Task<AuditLogFilterRuleDto> UpdateAsync(Guid id, UpdateAuditLogFilterRuleDto input)
        {
            var entity = await _ruleRepository.GetAsync(id);
            var beforeState = $"Enabled={entity.IsEnabled},Type={entity.RuleType},Pattern={entity.PathPattern}";
            
            entity.Update(input.Name, input.PathPattern, input.RuleType, input.Direction, input.HttpMethod, input.IsEnabled, input.Description);

            await _ruleRepository.UpdateAsync(entity, autoSave: true);
            await _localEventBus.PublishAsync(new AuditFilterRulesChangedEvent());

            var afterState = $"Enabled={entity.IsEnabled},Type={entity.RuleType},Pattern={entity.PathPattern}";
            _operationLogRecorder.Record(new SIASUN.RCS.Logs.OperatorLogs.OperationLogContext
            {
                Module = "Auditing",
                Action = "UpdateFilterRule",
                TargetType = "AuditFilterRule",
                TargetId = id.ToString(),
                BeforeState = beforeState,
                AfterState = afterState,
                Description = $"更新 API 审计过滤规则 [{entity.Name}]",
                Reason = $"运维人员修改 API 审计过滤规则 [{entity.Name}] 配置"
            });

            return ObjectMapper.Map<AuditLogFilterRule, AuditLogFilterRuleDto>(entity);
        }

        /// <summary>
        /// 切换 API 审计过滤规则的启用/禁用状态
        /// </summary>
        /// <param name="id">规则唯一标识</param>
        /// <returns>切换后的规则 DTO</returns>
        [Authorize(RCSPermissions.AuditLogFilterRules.Edit)]
        [OperationLog(Module = "Auditing", Action = "ToggleFilterRule", TargetType = "AuditFilterRule", Description = "切换 API 审计过滤规则启用状态")]
        public async Task<AuditLogFilterRuleDto> ToggleAsync(Guid id)
        {
            var entity = await _ruleRepository.GetAsync(id);
            var beforeState = entity.IsEnabled ? "Enabled" : "Disabled";
            entity.Toggle();
            await _ruleRepository.UpdateAsync(entity, autoSave: true);
            await _localEventBus.PublishAsync(new AuditFilterRulesChangedEvent());

            var afterState = entity.IsEnabled ? "Enabled" : "Disabled";
            _operationLogRecorder.Record(new SIASUN.RCS.Logs.OperatorLogs.OperationLogContext
            {
                Module = "Auditing",
                Action = "ToggleFilterRule",
                TargetType = "AuditFilterRule",
                TargetId = id.ToString(),
                BeforeState = beforeState,
                AfterState = afterState,
                Description = $"切换 API 审计过滤规则 [{entity.Name}] 状态为 [{afterState}]",
                Reason = $"运维人员切换 API 审计过滤规则 [{entity.Name}] 启用状态"
            });

            return ObjectMapper.Map<AuditLogFilterRule, AuditLogFilterRuleDto>(entity);
        }

        /// <summary>
        /// 删除指定的 API 审计过滤规则
        /// </summary>
        /// <param name="id">规则唯一标识</param>
        [Authorize(RCSPermissions.AuditLogFilterRules.Delete)]
        [OperationLog(Module = "Auditing", Action = "DeleteFilterRule", TargetType = "AuditFilterRule", Description = "删除 API 审计过滤规则")]
        public async Task DeleteAsync(Guid id)
        {
            var entity = await _ruleRepository.FindAsync(id);
            var ruleName = entity?.Name ?? id.ToString();

            await _ruleRepository.DeleteAsync(id, autoSave: true);
            await _localEventBus.PublishAsync(new AuditFilterRulesChangedEvent());

            _operationLogRecorder.Record(new SIASUN.RCS.Logs.OperatorLogs.OperationLogContext
            {
                Module = "Auditing",
                Action = "DeleteFilterRule",
                TargetType = "AuditFilterRule",
                TargetId = id.ToString(),
                BeforeState = "Existing",
                AfterState = "Deleted",
                Description = $"删除 API 审计过滤规则 [{ruleName}]",
                Reason = $"运维人员删除 API 审计过滤规则 [{ruleName}]"
            });
        }
    }
}
