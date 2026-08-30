using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SIASUN.RCS.Auditing
{
    public interface IEntityAuditLogStore
    {
        Task SaveBatchAsync(IReadOnlyList<EntityAuditLogEntry> entries, CancellationToken ct = default);
        Task PurgeBeforeAsync(DateTime expireTime, CancellationToken ct = default);
    }
}
