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
    /// <summary>
    /// EF Core 实体变更审计拦截器，负责在 SaveChanges 提交时截获实体属性变更并路由至审计通道
    /// 遵循规范三层审计与 L4 零丢失铁律，保障核心领域实体（AgvTask、AgvVehicle 等）变更不可抵赖
    /// </summary>
    public class EntityAuditInterceptor : SaveChangesInterceptor, ISingletonDependency
    {
        private readonly IServiceProvider _serviceProvider;
        private IEntityAuditLogChannel? _channel;
        private ICorrelationIdProvider? _correlationIdProvider;
        private IEntityAuditRuleEvaluator? _evaluator;
        private IMemoryCache? _memoryCache;

        /// <summary>
        /// 初始化实体变更审计拦截器
        /// </summary>
        /// <param name="serviceProvider">依赖注入服务提供者</param>
        public EntityAuditInterceptor(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // 使用 GetService（可选解析）：DbMigrator / 无 Logging 模块时返回 null 而不崩溃
        private IEntityAuditLogChannel? GetChannel() => _channel ??= _serviceProvider.GetService<IEntityAuditLogChannel>();
        private ICorrelationIdProvider? GetCorrelationIdProvider() => _correlationIdProvider ??= _serviceProvider.GetService<ICorrelationIdProvider>();
        private IEntityAuditRuleEvaluator? GetEvaluator() => _evaluator ??= _serviceProvider.GetService<IEntityAuditRuleEvaluator>();
        private IMemoryCache? GetMemoryCache() => _memoryCache ??= _serviceProvider.GetService<IMemoryCache>();

        /// <summary>
        /// 异步保存上下文更改时触发实体审计捕获
        /// </summary>
        /// <param name="eventData">EF Core 实体事件数据</param>
        /// <param name="result">拦截器执行结果</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>值任务</returns>
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            CaptureAuditLogs(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        /// <summary>
        /// 同步保存上下文更改时触发实体审计捕获
        /// </summary>
        /// <param name="eventData">EF Core 实体事件数据</param>
        /// <param name="result">拦截器执行结果</param>
        /// <returns>拦截器执行结果</returns>
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            CaptureAuditLogs(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        private void CaptureAuditLogs(DbContext? context)
        {
            if (context == null) return;

            var evaluator = GetEvaluator();
            if (evaluator == null) return; // DbMigrator / 无 Logging 模块时直接跳过

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
                    var memCache = GetMemoryCache();
                    if (memCache != null)
                    {
                        var entityIdStr = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "unknown";
                        var cacheKey = $"{fullName}_{entityIdStr}_{entry.State}";
                        
                        if (memCache.TryGetValue(cacheKey, out _))
                        {
                            continue; // 仍在采样冷却期内，抛弃该次审计
                        }
                        memCache.Set(cacheKey, true, TimeSpan.FromMilliseconds(ruleResult.SampleIntervalMs));
                    }
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

                if (changedProps.Count == 0) continue;

                var correlationIdProvider = GetCorrelationIdProvider();
                string? traceId = SIASUN.RCS.Diagnostics.RcsTraceContext.CurrentTraceId;
                if (string.IsNullOrWhiteSpace(traceId))
                {
                    traceId = correlationIdProvider?.Get();
                }

                if (string.IsNullOrWhiteSpace(traceId))
                {
                    try
                    {
                        var accessorType = Type.GetType("Microsoft.AspNetCore.Http.IHttpContextAccessor, Microsoft.AspNetCore.Http.Abstractions") 
                                        ?? Type.GetType("Microsoft.AspNetCore.Http.IHttpContextAccessor, Microsoft.AspNetCore.Http");
                        if (accessorType != null)
                        {
                            var accessor = _serviceProvider.GetService(accessorType);
                            if (accessor != null)
                            {
                                var httpContextProp = accessorType.GetProperty("HttpContext");
                                var httpContext = httpContextProp?.GetValue(accessor);
                                if (httpContext != null)
                                {
                                    var itemsProp = httpContext.GetType().GetProperty("Items");
                                    if (itemsProp?.GetValue(httpContext) is System.Collections.IDictionary items)
                                    {
                                        if (items.Contains("__RcsCorrelationId"))
                                        {
                                            traceId = items["__RcsCorrelationId"]?.ToString();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // 忽略反射回退解析异常
                    }
                }

                traceId ??= Guid.NewGuid().ToString("N");
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

                var channel = GetChannel();
                if (channel != null)
                {
                    var written = channel.TryWrite(msg);
                    if (!written)
                    {
                        try
                        {
                            channel.WriteAsync(msg).AsTask().Wait(TimeSpan.FromMilliseconds(500));
                        }
                        catch
                        {
                            // 吞掉等待异常，底层 Spill 机制负责最终兜底
                        }
                    }
                }
            }
        }
    }
}
