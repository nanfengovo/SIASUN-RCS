using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
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
        public async Task Should_Consume_Messages_And_Save()
        {
            var channel = new EntityAuditLogChannel();
            var mockStore = Substitute.For<IEntityAuditLogStore>();

            var consumer = new EntityAuditLogConsumer(
                channel,
                mockStore,
                NullLogger<EntityAuditLogConsumer>.Instance
            );

            var cts = new CancellationTokenSource();

            var task = consumer.StartAsync(cts.Token);

            var msg = new EntityAuditLogMessage
            {
                TraceId = "TEST-TRACE-1",
                EntityName = "TestEntity",
                EntityId = "T01",
                Action = "Modified",
                CreationTime = DateTime.UtcNow,
                ChangedProperties = new List<string> { "Name" }
            };

            channel.TryWrite(msg);

            await Task.Delay(200);
            
            cts.Cancel();
            try { await task; } catch { }

            // Assert
            await mockStore.Received(1).SaveBatchAsync(Arg.Any<IReadOnlyList<EntityAuditLogEntry>>(), Arg.Any<CancellationToken>());
        }
    }
}
