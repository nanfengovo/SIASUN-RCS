using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SIASUN.RCS.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace SIASUN.RCS.Infrastructure.Logging.Filtering
{
    public class EntityAuditRuleEvaluator : IEntityAuditRuleEvaluator, ISingletonDependency
    {
        private List<CompiledEntityAuditRule> _rules = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public EntityAuditRuleEvaluator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public EntityAuditRuleResult Evaluate(string fullName, string shortName)
        {
            var currentRules = _rules;
            foreach (var rule in currentRules)
            {
                if (rule.IsMatch(fullName, shortName))
                {
                    return new EntityAuditRuleResult(rule.Mode, rule.SampleIntervalMs, rule.ExcludedProperties);
                }
            }
            return new EntityAuditRuleResult(EntityAuditMode.Skip, 0, null); // 默认丢弃
        }

        public async Task RefreshRulesAsync()
        {
            await _lock.WaitAsync();
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IRepository<EntityAuditRule, Guid>>();
                
                var activeRules = await repository.GetListAsync(x => x.IsEnabled);
                
                // Priority 越小越优先，升序排序
                _rules = activeRules
                    .OrderBy(x => x.Priority)
                    .Select(x => new CompiledEntityAuditRule(x.EntityTypePattern, x.Mode, x.Priority, x.SampleIntervalMs, x.ExcludedProperties))
                    .ToList();
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
