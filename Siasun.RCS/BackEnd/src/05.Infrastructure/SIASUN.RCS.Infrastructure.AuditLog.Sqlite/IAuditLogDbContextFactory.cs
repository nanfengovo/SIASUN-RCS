using System;
using System.Threading.Tasks;

namespace SIASUN.RCS.Infrastructure.AuditLog.Sqlite
{
    public interface IAuditLogDbContextFactory
    {
        Task<AuditLogSqliteDbContext> CreateAsync(DateTime? time = null);
    }
}
