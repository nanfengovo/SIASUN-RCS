using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SIASUN.RCS.Auditing
{
    /// <summary>
    /// 报文持久化
    /// </summary>
    public interface IApiAuditLogStore
    {
        /// <summary>
        /// 批量保存API报文日志
        /// </summary>
        /// <param name="entries"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task SaveBatchAsync(IReadOnlyList<ApiAuditLogEntry> entries, CancellationToken ct = default);

        /// <summary>
        /// 定期清理过期的API报文日志
        /// </summary>
        /// <param name="expireTime"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
    }
}