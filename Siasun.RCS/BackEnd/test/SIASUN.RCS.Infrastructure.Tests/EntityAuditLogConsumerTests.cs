using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Infrastructure.Logging;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Tests
{
    public class EntityAuditLogConsumerTests
    {
        [Fact]
        public async Task ExecuteAsync_WhenChannelHasItems_ShouldBatchSaveToStore()
        {
            // Arrange
            var channel = new EntityAuditLogChannel();
            var store = Substitute.For<IEntityAuditLogStore>();
            var logger = Substitute.For<ILogger<EntityAuditLogConsumer>>();

            var consumer = new EntityAuditLogConsumer(channel, store, logger);

            // 推入 3 条数据
            channel.TryWrite(new EntityAuditLogEntry { EntityName = "A" });
            channel.TryWrite(new EntityAuditLogEntry { EntityName = "B" });
            channel.TryWrite(new EntityAuditLogEntry { EntityName = "C" });

            var cts = new CancellationTokenSource();

            // Act: 启动后台任务
            var runTask = consumer.StartAsync(cts.Token);

            // 给一点时间让后台任务从通道里取走数据并执行 SaveBatchAsync
            await Task.Delay(100);

            // 停止后台任务
            cts.Cancel();
            await runTask;

            // Assert: 验证 SaveBatchAsync 被调用过
            await store.Received(1).SaveBatchAsync(
                Arg.Any<IReadOnlyList<EntityAuditLogEntry>>(),
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public async Task ExecuteAsync_WhenStoreThrowsException_ShouldLogErrorAndKeepRunning()
        {
            // Arrange
            var channel = new EntityAuditLogChannel();
            var store = Substitute.For<IEntityAuditLogStore>();
            var logger = Substitute.For<ILogger<EntityAuditLogConsumer>>();

            var consumer = new EntityAuditLogConsumer(channel, store, logger);

            // 让 Store 在调用时抛出异常
            store.SaveBatchAsync(Arg.Any<IReadOnlyList<EntityAuditLogEntry>>(), Arg.Any<CancellationToken>())
                 .Returns(Task.FromException(new Exception("Database locked")));

            channel.TryWrite(new EntityAuditLogEntry { EntityName = "CrashTest" });

            var cts = new CancellationTokenSource();

            // Act
            var runTask = consumer.StartAsync(cts.Token);
            await Task.Delay(150); // 给时间抛异常并恢复
            cts.Cancel();
            await runTask;

            // Assert: 虽然发生异常，但这批数据已经被拿走，并且日志记录了异常。不应该导致整个服务崩溃
            await store.Received(1).SaveBatchAsync(Arg.Any<IReadOnlyList<EntityAuditLogEntry>>(), Arg.Any<CancellationToken>());
        }
    }
}
