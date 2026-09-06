using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SIASUN.RCS.Auditing;

namespace SIASUN.RCS.Infrastructure.AuditLog.Sqlite
{
    /// <summary>
    /// 基于 SQLite 分片文件的实体变更审计日志底层存储仓库实现
    /// </summary>
    public class SqliteEntityAuditLogStore : IEntityAuditLogStore, Volo.Abp.DependencyInjection.ISingletonDependency
    {
        private readonly IAuditLogDbContextFactory _dbContextFactory;

        /// <summary>
        /// 构造函数注入分片 DbContext 工厂
        /// </summary>
        public SqliteEntityAuditLogStore(IAuditLogDbContextFactory dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }


        /// <summary>
        /// 批量保存实体变更审计日志
        /// </summary>
        public async Task SaveBatchAsync(IReadOnlyList<EntityAuditLogEntry> entries, CancellationToken ct = default)
        {
            if (entries == null || entries.Count == 0) return;

            var time = entries[0].CreationTime;
            await using var dbContext = await _dbContextFactory.CreateAsync(time);

            await dbContext.EntityAuditLogs.AddRangeAsync(entries, ct);
            await dbContext.SaveChangesAsync(ct);
        }

        /// <summary>
        /// 按时间范围检索实体变更审计日志（支持跨月度分片聚合与 TraceId/EntityName 过滤）
        /// </summary>
        /// <param name="startTime">起始时间 (UTC)</param>
        /// <param name="endTime">结束时间 (UTC)</param>
        /// <param name="keyword">关键词过滤 (匹配 TraceId 或 EntityName)</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>实体审计日志列表</returns>
        public async Task<IReadOnlyList<EntityAuditLogEntry>> GetListAsync(DateTime startTime, DateTime endTime, string? keyword = null, CancellationToken ct = default)
        {
            var result = new List<EntityAuditLogEntry>();
            var currentMonth = new DateTime(startTime.Year, startTime.Month, 1);
            var endMonth = new DateTime(endTime.Year, endTime.Month, 1);

            while (currentMonth <= endMonth)
            {
                try
                {
                    await using var dbContext = await _dbContextFactory.CreateAsync(currentMonth);
                    var query = dbContext.EntityAuditLogs
                        .Where(x => x.CreationTime >= startTime && x.CreationTime <= endTime);

                    if (!string.IsNullOrEmpty(keyword))
                    {
                        query = query.Where(x => x.TraceId == keyword || x.EntityName.Contains(keyword) || x.EntityId == keyword);
                    }

                    var entries = await query.OrderBy(x => x.CreationTime).ToListAsync(ct);
                    result.AddRange(entries);
                }
                catch
                {
                    // 忽略不存在或已归档的分片月度数据库
                }

                currentMonth = currentMonth.AddMonths(1);
            }

            return result.OrderBy(x => x.CreationTime).ToList();
        }
    }
}
