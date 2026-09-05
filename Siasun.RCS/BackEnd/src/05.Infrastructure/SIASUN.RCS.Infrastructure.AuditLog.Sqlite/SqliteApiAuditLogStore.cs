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

        public async Task<IReadOnlyList<ApiAuditLogEntry>> GetListAsync(DateTime startTime, DateTime endTime, string? keyword = null, CancellationToken ct = default)
        {
            var result = new List<ApiAuditLogEntry>();
            var currentMonth = new DateTime(startTime.Year, startTime.Month, 1);
            var endMonth = new DateTime(endTime.Year, endTime.Month, 1);

            while (currentMonth <= endMonth)
            {
                try
                {
                    await using var dbContext = await _dbContextFactory.CreateAsync(currentMonth);
                    var query = dbContext.ApiAuditLogs
                        .Where(x => x.CreationTime >= startTime && x.CreationTime <= endTime);

                    if (!string.IsNullOrEmpty(keyword))
                    {
                        query = query.Where(x => x.TraceId == keyword || x.Path.Contains(keyword));
                    }

                    var entries = await query.OrderBy(x => x.CreationTime).ToListAsync(ct);
                    result.AddRange(entries);
                }
                catch
                {
                    // 忽略不存在的月度数据库
                }

                currentMonth = currentMonth.AddMonths(1);
            }

            return result.OrderBy(x => x.CreationTime).ToList();
        }
    }
}
