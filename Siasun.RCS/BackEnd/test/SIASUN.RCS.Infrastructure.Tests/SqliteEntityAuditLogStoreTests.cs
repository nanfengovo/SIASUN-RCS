using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Infrastructure.AuditLog.Sqlite;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Tests
{
    public class SqliteEntityAuditLogStoreTests
    {
        [Fact]
        public async Task SaveBatchAsync_And_Purge_ShouldWorkCorrectly()
        {
            var testDbPath = Path.Combine(Path.GetTempPath(), $"test_entity_audit_{Guid.NewGuid():N}.db");
            var services = new ServiceCollection();

            services.AddDbContext<AuditLogSqliteDbContext>(options =>
            {
                options.UseSqlite($"Data Source={testDbPath};");
            });

            var serviceProvider = services.BuildServiceProvider();
            using (var scope = serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AuditLogSqliteDbContext>();
                db.Database.EnsureCreated();
            }

            var store = new SqliteEntityAuditLogStore(
                serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<SqliteEntityAuditLogStore>.Instance
            );

            var timeNow = DateTime.UtcNow;

            var entries = new List<EntityAuditLogEntry>
            {
                new()
                {
                    TraceId = "TRACE-ENTITY-1",
                    EntityName = "AgvTask",
                    EntityId = "1001",
                    Action = "Modified",
                    PropertyChangesJson = "{\"State\":{\"Old\":\"Pending\",\"New\":\"Running\"}}",
                    CreationTime = timeNow
                }
            };

            // Act: Insert
            await store.SaveBatchAsync(entries);

            // Assert
            using (var scope = serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AuditLogSqliteDbContext>();
                var saved = await db.Set<EntityAuditLogEntry>().FirstOrDefaultAsync(x => x.TraceId == "TRACE-ENTITY-1");
                saved.ShouldNotBeNull();
                saved.EntityName.ShouldBe("AgvTask");
                saved.Action.ShouldBe("Modified");
                saved.PropertyChangesJson.ShouldContain("Running");
            }

            // Act 2: Purge
            await store.PurgeBeforeAsync(timeNow.AddMinutes(1));

            // Assert
            using (var scope = serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AuditLogSqliteDbContext>();
                var count = await db.Set<EntityAuditLogEntry>().CountAsync();
                count.ShouldBe(0); // 应该被删除了
            }

            // Cleanup
            if (File.Exists(testDbPath))
            {
                File.Delete(testDbPath);
            }
        }
    }
}

