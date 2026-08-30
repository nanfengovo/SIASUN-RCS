using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Quartz;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Infrastructure.AuditLog.Sqlite;
using Volo.Abp.DependencyInjection;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Tests.AuditLog.Sqlite;

public class ApiAuditLogCleanupWorkerTests
{
    private IConfiguration BuildConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string> {
            {"AuditLog:RetainDays", "30"}
        };
        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public async Task Execute_Should_Purge_Logs()
    {
        // Arrange
        var worker = new ApiAuditLogCleanupWorker();
        var store = Substitute.For<IApiAuditLogStore>();
        var configuration = BuildConfiguration();
        var lazyServiceProvider = Substitute.For<IAbpLazyServiceProvider>();
        var jobContext = Substitute.For<IJobExecutionContext>();

        lazyServiceProvider.LazyGetRequiredService<IApiAuditLogStore>().Returns(store);
        lazyServiceProvider.LazyGetRequiredService<IConfiguration>().Returns(configuration);
        
        worker.LazyServiceProvider = lazyServiceProvider;
        
        jobContext.CancellationToken.Returns(CancellationToken.None);

        // Act
        await worker.Execute(jobContext);

        // Assert
        await store.Received(1).PurgeBeforeAsync(Arg.Any<DateTime>(), CancellationToken.None);
    }

    [Fact]
    public async Task Execute_Should_Throw_And_Log_Exception()
    {
        // Arrange
        var worker = new ApiAuditLogCleanupWorker();
        var store = Substitute.For<IApiAuditLogStore>();
        var configuration = BuildConfiguration();
        var lazyServiceProvider = Substitute.For<IAbpLazyServiceProvider>();
        var jobContext = Substitute.For<IJobExecutionContext>();

        lazyServiceProvider.LazyGetRequiredService<IApiAuditLogStore>().Returns(store);
        lazyServiceProvider.LazyGetRequiredService<IConfiguration>().Returns(configuration);
        
        worker.LazyServiceProvider = lazyServiceProvider;
        
        store.PurgeBeforeAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new Exception("Test exception")));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => await worker.Execute(jobContext));
    }
}
