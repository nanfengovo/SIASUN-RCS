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
using HttpMethod = SIASUN.RCS.Auditing.HttpMethod;

namespace SIASUN.RCS.Infrastructure.Tests;

public class SqliteApiAuditLogStoreTests
{
    [Fact]
    public async Task SaveBatchAsync_WithRealSqlite_ShouldInsertSuccessfully()
    {
        var testDbPath = Path.Combine(Path.GetTempPath(), $"test_audit_{Guid.NewGuid():N}.db");
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

        var store = new SqliteApiAuditLogStore(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SqliteApiAuditLogStore>.Instance
        );

        var entries = new List<ApiAuditLogEntry>
        {
            new()
            {
                TraceId = "TR-1001",
                Direction = Direction.Inbound,
                Peer = "TM",
                HttpMethod = HttpMethod.Post,
                Path = "/api/v1/xinsong/task_arrive",
                StatusCode = 200,
                ElapsedMs = 45,
                RequestBody = "{\"test\":true}",
                ResponseBody = "{\"ok\":true}",
                ClientIpAddress = "127.0.0.1",
                ClientName = "TM-Client",
                CreationTime = DateTime.UtcNow
            }
        };

        // Act
        await store.SaveBatchAsync(entries);

        // Assert
        using (var scope = serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuditLogSqliteDbContext>();
            var saved = await db.ApiAuditLogs.FirstOrDefaultAsync(x => x.TraceId == "TR-1001");
            saved.ShouldNotBeNull();
            saved.Path.ShouldBe("/api/v1/xinsong/task_arrive");
            saved.Peer.ShouldBe("TM");
        }

        // Act 2: Purge
        await store.PurgeBeforeAsync(DateTime.UtcNow.AddMinutes(1));

        using (var scope = serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuditLogSqliteDbContext>();
            var count = await db.ApiAuditLogs.CountAsync();
            count.ShouldBe(0);
        }

        // Cleanup
        if (File.Exists(testDbPath))
        {
            File.Delete(testDbPath);
        }
    }
}
