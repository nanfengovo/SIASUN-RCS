using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Auditing;

namespace SIASUN.RCS.Infrastructure.AuditLog.Sqlite
{
    public class SqliteApiAuditLogStore : IApiAuditLogStore
    {

        private readonly IServiceScopeFactory _scopeFactory;

        private readonly ILogger<SqliteApiAuditLogStore> _logger;

        public SqliteApiAuditLogStore(IServiceScopeFactory scopeFactory, ILogger<SqliteApiAuditLogStore> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task PurgeBeforeAsync(DateTime expireTime, CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AuditLogSqliteDbContext>();

            await dbContext.ApiAuditLogs.Where(x => x.CreationTime < expireTime).ExecuteDeleteAsync(ct);
        }

        public async Task SaveBatchAsync(IReadOnlyList<ApiAuditLogEntry> entries, CancellationToken ct = default)
        {
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AuditLogSqliteDbContext>();

            await dbContext.ApiAuditLogs.AddRangeAsync(entries, ct);
            await dbContext.SaveChangesAsync(ct);
        }
    }
}