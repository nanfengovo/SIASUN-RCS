using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SIASUN.RCS.Auditing
{
    /// <summary>
    /// 实体变更审计日志底层存储仓库接口
    /// 提供实体审计日志的批量持久化与跨时间范围多源分片检索能力
    /// </summary>
    public interface IEntityAuditLogStore
    {
        /// <summary>
        /// 批量持久化实体变更审计日志
        /// </summary>
        /// <param name="entries">实体审计条目集合</param>
        /// <param name="ct">取消令牌</param>
        Task SaveBatchAsync(IReadOnlyList<EntityAuditLogEntry> entries, CancellationToken ct = default);

        /// <summary>
        /// 按时间范围检索实体变更审计日志（支持按 TraceId 或实体名过滤）
        /// </summary>
        /// <param name="startTime">起始时间 (UTC)</param>
        /// <param name="endTime">结束时间 (UTC)</param>
        /// <param name="keyword">关键词过滤（可选匹配 TraceId 或 EntityName）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>实体审计日志列表</returns>
        Task<IReadOnlyList<EntityAuditLogEntry>> GetListAsync(
            DateTime startTime,
            DateTime endTime,
            string? keyword = null,
            CancellationToken ct = default);
    }
}
