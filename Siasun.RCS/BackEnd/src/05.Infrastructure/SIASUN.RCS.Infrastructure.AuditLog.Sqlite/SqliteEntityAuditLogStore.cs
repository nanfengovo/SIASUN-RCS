using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SIASUN.RCS.Auditing;

namespace SIASUN.RCS.Infrastructure.AuditLog.Sqlite
{
    public class SqliteEntityAuditLogStore : IEntityAuditLogStore, Volo.Abp.DependencyInjection.ISingletonDependency
    {
        private readonly IAuditLogDbContextFactory _dbContextFactory;

        public SqliteEntityAuditLogStore(IAuditLogDbContextFactory dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task PurgeBeforeAsync(DateTime expireTime, CancellationToken ct = default)
        {
            // 由 CleanupWorker 基于文件删除完成
            await Task.CompletedTask;
        }

        public async Task SaveBatchAsync(IReadOnlyList<EntityAuditLogEntry> entries, CancellationToken ct = default)
        {
            if (entries == null || entries.Count == 0) return;

            var time = entries[0].CreationTime;
            await using var dbContext = await _dbContextFactory.CreateAsync(time);

            await dbContext.EntityAuditLogs.AddRangeAsync(entries, ct);
            await dbContext.SaveChangesAsync(ct);
        }
    }
}
