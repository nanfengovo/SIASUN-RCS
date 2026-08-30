using System;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace SIASUN.RCS.Auditing
{
    public class EntityAuditRuleDataSeedContributor : IDataSeedContributor, ITransientDependency
    {
        private readonly IRepository<EntityAuditRule, Guid> _ruleRepository;
        private readonly IGuidGenerator _guidGenerator;

        public EntityAuditRuleDataSeedContributor(
            IRepository<EntityAuditRule, Guid> ruleRepository,
            IGuidGenerator guidGenerator)
        {
            _ruleRepository = ruleRepository;
            _guidGenerator = guidGenerator;
        }

        public async Task SeedAsync(DataSeedContext context)
        {
            if (await _ruleRepository.GetCountAsync() > 0)
            {
                return;
            }

            // ABP 基础设施相关表，默认 Skip
            await _ruleRepository.InsertAsync(new EntityAuditRule(
                id: _guidGenerator.Create(),
                name: "Skip Identity",
                entityTypePattern: "Volo.Abp.Identity.*",
                mode: EntityAuditMode.Skip,
                sampleIntervalMs: 0,
                excludedProperties: null,
                priority: 100,
                isEnabled: true
            ));

            await _ruleRepository.InsertAsync(new EntityAuditRule(
                id: _guidGenerator.Create(),
                name: "Skip OpenIddict",
                entityTypePattern: "Volo.Abp.OpenIddict.*",
                mode: EntityAuditMode.Skip,
                sampleIntervalMs: 0,
                excludedProperties: null,
                priority: 100,
                isEnabled: true
            ));

            await _ruleRepository.InsertAsync(new EntityAuditRule(
                id: _guidGenerator.Create(),
                name: "Skip Auditing",
                entityTypePattern: "Volo.Abp.Auditing.*",
                mode: EntityAuditMode.Skip,
                sampleIntervalMs: 0,
                excludedProperties: null,
                priority: 100,
                isEnabled: true
            ));
            
            // 全局兜底规则：默认 Skip（优先级 9999）
            await _ruleRepository.InsertAsync(new EntityAuditRule(
                id: _guidGenerator.Create(),
                name: "Default Fallback Skip",
                entityTypePattern: "*",
                mode: EntityAuditMode.Skip,
                sampleIntervalMs: 0,
                excludedProperties: null,
                priority: 9999,
                isEnabled: true
            ));
        }
    }
}
