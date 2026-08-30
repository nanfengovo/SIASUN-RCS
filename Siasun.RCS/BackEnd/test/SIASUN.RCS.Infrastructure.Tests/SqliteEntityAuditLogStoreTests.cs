using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
            var factory = new AuditLogDbContextFactory();

            var store = new SqliteEntityAuditLogStore(factory);

            var timeNow = new DateTime(2099, 1, 1, 12, 0, 0, DateTimeKind.Utc); // 隔离文件

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

            await store.SaveBatchAsync(entries);

            await using (var db = await factory.CreateAsync(timeNow))
            {
                var saved = await db.Set<EntityAuditLogEntry>().FirstOrDefaultAsync(x => x.TraceId == "TRACE-ENTITY-1");
                saved.ShouldNotBeNull();
                saved.EntityName.ShouldBe("AgvTask");
                saved.Action.ShouldBe("Modified");
                saved.PropertyChangesJson.ShouldContain("Running");
            }
        }
    }
}
