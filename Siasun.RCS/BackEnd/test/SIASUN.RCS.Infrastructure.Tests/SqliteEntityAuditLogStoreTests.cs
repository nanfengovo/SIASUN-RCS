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

        [Fact]
        public async Task GetListAsync_ShouldQueryAndFilterAcrossDates()
        {
            var factory = new AuditLogDbContextFactory();
            var store = new SqliteEntityAuditLogStore(factory);

            var testTag = $"TEST_{Guid.NewGuid():N}";
            var t1 = new DateTime(2097, 5, 1, 10, 0, 0, DateTimeKind.Utc);
            var t2 = new DateTime(2097, 5, 15, 10, 0, 0, DateTimeKind.Utc);
            var t3 = new DateTime(2097, 6, 1, 10, 0, 0, DateTimeKind.Utc);

            var entries = new List<EntityAuditLogEntry>
            {
                new()
                {
                    TraceId = $"{testTag}_1",
                    EntityName = $"AgvTask_{testTag}",
                    EntityId = $"TASK_{testTag}",
                    Action = "Created",
                    CreationTime = t1
                },
                new()
                {
                    TraceId = $"{testTag}_2",
                    EntityName = $"AgvTask_{testTag}",
                    EntityId = $"TASK_{testTag}",
                    Action = "Updated",
                    CreationTime = t2
                },
                new()
                {
                    TraceId = $"{testTag}_3",
                    EntityName = $"Vehicle_{testTag}",
                    EntityId = $"AGV_{testTag}",
                    Action = "Updated",
                    CreationTime = t3
                }
            };

            await store.SaveBatchAsync(entries);

            // Query by keyword spanning both months
            var allResult = await store.GetListAsync(new DateTime(2097, 5, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2097, 6, 2, 0, 0, 0, DateTimeKind.Utc), keyword: testTag);
            allResult.Count.ShouldBe(3);

            // Filter by entityName
            var taskResult = await store.GetListAsync(new DateTime(2097, 5, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2097, 6, 2, 0, 0, 0, DateTimeKind.Utc), keyword: $"AgvTask_{testTag}");
            taskResult.Count.ShouldBe(2);

            // Filter by entityId
            var idResult = await store.GetListAsync(new DateTime(2097, 5, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2097, 6, 2, 0, 0, 0, DateTimeKind.Utc), keyword: $"AGV_{testTag}");
            idResult.Count.ShouldBe(1);
            idResult[0].TraceId.ShouldBe($"{testTag}_3");

            // Filter by traceId
            var traceResult = await store.GetListAsync(new DateTime(2097, 5, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2097, 6, 2, 0, 0, 0, DateTimeKind.Utc), keyword: $"{testTag}_1");
            traceResult.Count.ShouldBe(1);
            traceResult[0].Action.ShouldBe("Created");
        }
    }
}
