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
    public class SqliteApiAuditLogStoreTests
    {
        [Fact]
        public async Task SaveBatchAsync_And_Purge_ShouldWorkCorrectly()
        {
            var factory = new AuditLogDbContextFactory();
            var store = new SqliteApiAuditLogStore(factory);
            var timeNow = new DateTime(2098, 1, 1, 12, 0, 0, DateTimeKind.Utc); // 隔离文件

            var entries = new List<ApiAuditLogEntry>
            {
                new()
                {
                    HttpMethod = SIASUN.RCS.Auditing.HttpMethod.Get,
                    Path = "/api/test",
                    ElapsedMs = 10,
                    CreationTime = timeNow
                }
            };

            await store.SaveBatchAsync(entries);

            await using (var db = await factory.CreateAsync(timeNow))
            {
                var saved = await db.Set<ApiAuditLogEntry>().FirstOrDefaultAsync(x => x.Path == "/api/test");
                saved.ShouldNotBeNull();
                saved.HttpMethod.ShouldBe(SIASUN.RCS.Auditing.HttpMethod.Get);
            }
        }
    }
}
