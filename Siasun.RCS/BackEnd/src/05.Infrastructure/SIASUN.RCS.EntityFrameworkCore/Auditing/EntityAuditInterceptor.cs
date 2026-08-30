using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using SIASUN.RCS.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Tracing;

namespace SIASUN.RCS.EntityFrameworkCore.Auditing
{
    public class EntityAuditInterceptor : SaveChangesInterceptor, ISingletonDependency
    {
        private readonly IServiceProvider _serviceProvider;
        private IEntityAuditLogChannel? _channel;
        private ICorrelationIdProvider? _correlationIdProvider;
        private IEntityAuditRuleEvaluator? _evaluator;
        private IMemoryCache? _memoryCache;

        public EntityAuditInterceptor(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private IEntityAuditLogChannel GetChannel() => _channel ??= _serviceProvider.GetRequiredService<IEntityAuditLogChannel>();
        private ICorrelationIdProvider GetCorrelationIdProvider() => _correlationIdProvider ??= _serviceProvider.GetRequiredService<ICorrelationIdProvider>();
        private IEntityAuditRuleEvaluator GetEvaluator() => _evaluator ??= _serviceProvider.GetRequiredService<IEntityAuditRuleEvaluator>();
        private IMemoryCache GetMemoryCache() => _memoryCache ??= _serviceProvider.GetRequiredService<IMemoryCache>();

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            CaptureAuditLogs(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            CaptureAuditLogs(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        private void CaptureAuditLogs(DbContext? context)
        {
            if (context == null) return;

            var evaluator = GetEvaluator();

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;
                
                var entityType = entry.Entity.GetType();
                var shortName = entityType.Name;
                if (shortName == "AuditLogFilterRule" || shortName == "EntityAuditRule" || shortName == "EntityAuditLogEntry" || shortName == "ApiAuditLogEntry")
                    continue;

                var fullName = entityType.FullName ?? shortName;
                var ruleResult = evaluator.Evaluate(fullName, shortName);

                if (ruleResult.Mode == EntityAuditMode.Skip)
                    continue;

                // 检查采样频率
                if (ruleResult.SampleIntervalMs > 0)
                {
                    var entityIdStr = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "unknown";
                    var cacheKey = $"{fullName}_{entityIdStr}_{entry.State}";
                    
                    if (GetMemoryCache().TryGetValue(cacheKey, out _))
                    {
                        continue; // 仍在采样冷却期内，抛弃该次审计
                    }
                    GetMemoryCache().Set(cacheKey, true, TimeSpan.FromMilliseconds(ruleResult.SampleIntervalMs));
                }

                var excludedProps = ruleResult.ExcludedProperties?.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();

                var changedProps = new List<string>();

                var originalValues = new Dictionary<string, object?>();
                var currentValues = new Dictionary<string, object?>();

                foreach (var prop in entry.Properties)
                {
                    if (prop.IsModified || entry.State == EntityState.Added || entry.State == EntityState.Deleted)
                    {
                        if (excludedProps.Contains(prop.Metadata.Name))
                            continue;

                        changedProps.Add(prop.Metadata.Name);

                        if (ruleResult.Mode == EntityAuditMode.Full)
                        {
                            if (entry.State == EntityState.Modified)
                            {
                                originalValues[prop.Metadata.Name] = prop.OriginalValue;
                                currentValues[prop.Metadata.Name] = prop.CurrentValue;
                            }
                            else
                            {
                                originalValues[prop.Metadata.Name] = null;
                                currentValues[prop.Metadata.Name] = prop.CurrentValue;
                            }
                        }
                    }
                }

                // 如果没有任何改变，忽略
                if (changedProps.Count == 0) continue;

                var traceId = GetCorrelationIdProvider().Get() ?? Guid.NewGuid().ToString("N");
                var pkProp = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                var pkValue = pkProp?.CurrentValue?.ToString() ?? "0";

                var msg = new EntityAuditLogMessage
                {
                    TraceId = traceId,
                    EntityName = shortName,
                    EntityId = pkValue,
                    Action = entry.State.ToString(),
                    ChangedProperties = changedProps,
                    OriginalValues = ruleResult.Mode == EntityAuditMode.Full ? originalValues : null,
                    CurrentValues = ruleResult.Mode == EntityAuditMode.Full ? currentValues : null,
                    CreationTime = DateTime.UtcNow
                };

                GetChannel().TryWrite(msg);
            }
        }
    }
}
