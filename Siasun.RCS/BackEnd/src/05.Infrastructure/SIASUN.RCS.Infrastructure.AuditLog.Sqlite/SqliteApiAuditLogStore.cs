using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SIASUN.RCS.Auditing;

namespace SIASUN.RCS.Infrastructure.AuditLog.Sqlite
{
    public class SqliteApiAuditLogStore : IApiAuditLogStore, Volo.Abp.DependencyInjection.ISingletonDependency
    {
        private readonly IAuditLogDbContextFactory _dbContextFactory;

        public SqliteApiAuditLogStore(IAuditLogDbContextFactory dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }


        public async Task SaveBatchAsync(IReadOnlyList<ApiAuditLogEntry> entries, CancellationToken ct = default)
        {
            if (entries == null || entries.Count == 0) return;

            // 根据当批次第一条数据的时间决定写哪个库（理论上同批次时间极近）
            var time = entries[0].CreationTime;
            await using var dbContext = await _dbContextFactory.CreateAsync(time);

            await dbContext.ApiAuditLogs.AddRangeAsync(entries, ct);
            await dbContext.SaveChangesAsync(ct);
        }
    }
}
