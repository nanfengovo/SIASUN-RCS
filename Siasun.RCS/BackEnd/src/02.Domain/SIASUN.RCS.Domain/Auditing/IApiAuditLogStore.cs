using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SIASUN.RCS.Auditing
{
    /// <summary>
    /// API 报文审计日志底层存储仓库接口
    /// </summary>
    public interface IApiAuditLogStore
    {
        /// <summary>
        /// 批量持久化 API 报文审计日志
        /// </summary>
        /// <param name="entries">报文审计日志条目集合</param>
        /// <param name="ct">取消令牌</param>
        Task SaveBatchAsync(IReadOnlyList<ApiAuditLogEntry> entries, CancellationToken ct = default);

        /// <summary>
        /// 按时间范围与关键字检索 API 报文日志
        /// </summary>
        /// <param name="startTime">起始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="keyword">关键词过滤 (匹配 Path、TraceId 等)</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>审计日志实体列表</returns>
        Task<IReadOnlyList<ApiAuditLogEntry>> GetListAsync(
            DateTime startTime,
            DateTime endTime,
            string? keyword = null,
            CancellationToken ct = default);
    }
}
