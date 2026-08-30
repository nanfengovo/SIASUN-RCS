using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SIASUN.RCS.Auditing;
using Volo.Abp.Tracing;

namespace SIASUN.RCS.EntityFrameworkCore.Auditing
{
    public class EntityAuditInterceptor : SaveChangesInterceptor
    {
        private readonly IServiceProvider _serviceProvider;

        public EntityAuditInterceptor(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, 
            InterceptionResult<int> result, 
            CancellationToken cancellationToken = default)
        {
            InterceptEntityChanges(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData, 
            InterceptionResult<int> result)
        {
            InterceptEntityChanges(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        private void InterceptEntityChanges(DbContext? context)
        {
            if (context == null) return;

            var channel = _serviceProvider.GetService<EntityAuditLogChannel>();
            if (channel == null) return;

            var correlationIdProvider = _serviceProvider.GetService<ICorrelationIdProvider>();
            var traceId = correlationIdProvider?.Get() ?? string.Empty;

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
                {
                    if (entry.Entity.GetType().Name.Contains("AuditLog")) continue;

                    var changesDict = new Dictionary<string, object?>();

                    foreach (var prop in entry.Properties)
                    {
                        if (prop.IsModified || entry.State == EntityState.Added || entry.State == EntityState.Deleted)
                        {
                            changesDict[prop.Metadata.Name] = new
                            {
                                Old = entry.State == EntityState.Added ? null : prop.OriginalValue,
                                New = entry.State == EntityState.Deleted ? null : prop.CurrentValue
                            };
                        }
                    }

                    var logEntry = new EntityAuditLogEntry
                    {
                        TraceId = traceId,
                        EntityName = entry.Metadata.ShortName(),
                        EntityId = entry.Metadata.FindPrimaryKey()?.Properties[0]?.PropertyInfo?.GetValue(entry.Entity)?.ToString() ?? "Unknown",
                        Action = entry.State.ToString(),
                        PropertyChangesJson = JsonSerializer.Serialize(changesDict),
                        CreationTime = DateTime.UtcNow
                    };

                    channel.TryWrite(logEntry);
                }
            }
        }
    }
}
