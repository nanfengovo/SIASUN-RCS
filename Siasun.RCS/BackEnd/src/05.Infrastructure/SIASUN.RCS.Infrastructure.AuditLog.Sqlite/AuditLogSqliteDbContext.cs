using Microsoft.EntityFrameworkCore;
using SIASUN.RCS.Auditing;

namespace SIASUN.RCS.Infrastructure.AuditLog.Sqlite
{
    public class AuditLogSqliteDbContext : DbContext
    {
        public DbSet<ApiAuditLogEntry> ApiAuditLogs { get; set; } = null!;
        public DbSet<EntityAuditLogEntry> EntityAuditLogs { get; set; } = null!;

        public AuditLogSqliteDbContext(DbContextOptions<AuditLogSqliteDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<ApiAuditLogEntry>(b =>
            {
                b.ToTable("ApiAuditLogs");
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedOnAdd(); // SQLite 自增主键
                // 核心索引：支撑按时间、TraceId、对端系统秒级检索
                b.HasIndex(x => x.CreationTime);
                b.HasIndex(x => x.TraceId);
                b.HasIndex(x => new { x.Direction, x.Peer, x.CreationTime });
                // 限制字段长度，优化 SQLite 存储
                b.Property(x => x.TraceId).HasMaxLength(64);
                b.Property(x => x.Peer).HasMaxLength(32);
                b.Property(x => x.Path).HasMaxLength(256);
                b.Property(x => x.ClientIpAddress).HasMaxLength(64);
                b.Property(x => x.ClientName).HasMaxLength(64);
            });

            builder.Entity<EntityAuditLogEntry>(b =>
            {
                b.ToTable("EntityAuditLogs");
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedOnAdd();
                b.HasIndex(x => x.CreationTime);
                b.HasIndex(x => x.TraceId);
                b.HasIndex(x => x.EntityName);

                b.Property(x => x.TraceId).HasMaxLength(64);
                b.Property(x => x.EntityName).HasMaxLength(128);
                b.Property(x => x.EntityId).HasMaxLength(128);
                b.Property(x => x.Action).HasMaxLength(32);
            });
        }
    }
}