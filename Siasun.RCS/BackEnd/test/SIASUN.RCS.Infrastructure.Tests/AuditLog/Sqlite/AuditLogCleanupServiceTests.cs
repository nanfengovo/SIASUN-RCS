using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Infrastructure.AuditLog.Sqlite;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Tests.AuditLog.Sqlite
{
    public class AuditLogCleanupServiceTests
    {
        [Fact]
        public async Task CleanExpiredFilesAsync_WhenLogDirDoesNotExist_ShouldNotThrow()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AuditLog:RetainDays"] = "30"
                })
                .Build();

            var logger = Substitute.For<ILogger<AuditLogCleanupService>>();
            var service = new AuditLogCleanupService(config, logger);

            var exception = await Record.ExceptionAsync(() => service.CleanExpiredFilesAsync());
            exception.ShouldBeNull();
        }

        [Fact]
        public async Task CleanExpiredFilesAsync_WithExpiredFiles_ShouldDeleteFiles()
        {
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(logDir);

            var oldFile = Path.Combine(logDir, "api_audit_log_202001.db");
            var oldWal = Path.Combine(logDir, "api_audit_log_202001.db-wal");
            var currentFile = Path.Combine(logDir, $"api_audit_log_{DateTime.UtcNow:yyyyMM}.db");

            try
            {
                await File.WriteAllTextAsync(oldFile, "test");
                await File.WriteAllTextAsync(oldWal, "test");
                await File.WriteAllTextAsync(currentFile, "test");

                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["AuditLog:RetainDays"] = "30"
                    })
                    .Build();

                var logger = Substitute.For<ILogger<AuditLogCleanupService>>();
                var service = new AuditLogCleanupService(config, logger);

                await service.CleanExpiredFilesAsync();

                File.Exists(oldFile).ShouldBeFalse();
                File.Exists(oldWal).ShouldBeFalse();
                File.Exists(currentFile).ShouldBeTrue();
            }
            finally
            {
                if (File.Exists(oldFile)) File.Delete(oldFile);
                if (File.Exists(oldWal)) File.Delete(oldWal);
                if (File.Exists(currentFile)) File.Delete(currentFile);
            }
        }

        [Fact]
        public async Task CleanByCapacityAsync_WhenFreeSpaceIsSufficient_ShouldReturnEmpty()
        {
            var config = new ConfigurationBuilder().Build();
            var logger = Substitute.For<ILogger<AuditLogCleanupService>>();
            var service = new AuditLogCleanupService(config, logger);

            // Target 1 byte of free space, which any drive should have
            var deleted = await service.CleanByCapacityAsync(1, 24);
            deleted.ShouldBeEmpty();
        }

        [Fact]
        public async Task AuditLogCleanupJob_Execute_ShouldExecuteSuccessfully()
        {
            var config = new ConfigurationBuilder().Build();
            var logger = Substitute.For<ILogger<AuditLogCleanupService>>();
            var service = new AuditLogCleanupService(config, logger);
            var job = new AuditLogCleanupJob(service);

            var context = Substitute.For<Quartz.IJobExecutionContext>();
            context.CancellationToken.Returns(CancellationToken.None);

            var exception = await Record.ExceptionAsync(() => job.Execute(context));
            exception.ShouldBeNull();
        }
    }
}
