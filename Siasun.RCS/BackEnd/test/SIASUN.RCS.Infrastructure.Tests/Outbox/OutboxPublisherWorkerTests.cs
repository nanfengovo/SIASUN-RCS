using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using SIASUN.RCS.Infrastructure.BackgroundJobs.Outbox;
using SIASUN.RCS.Outbox;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Tests.Outbox
{
    /// <summary>
    /// Outbox 消息发布后台工作者与 Polly 弹性机制单元测试
    /// </summary>
    public class OutboxPublisherWorkerTests
    {
        /// <summary>
        /// 测试在消息投递成功时，消息状态被正确更新为 Published 并完成工作单元
        /// </summary>
        [Fact]
        public async Task ProcessBatchAsync_Should_Dispatch_Pending_Messages_And_Mark_Published()
        {
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            var scope = Substitute.For<IServiceScope>();
            var serviceProvider = Substitute.For<IServiceProvider>();
            var uowManager = Substitute.For<IUnitOfWorkManager>();
            var uow = Substitute.For<IUnitOfWork>();
            var repository = Substitute.For<IRepository<OutboxMessage, Guid>>();
            var dispatcher = Substitute.For<IOutboxMessageDispatcher>();

            scopeFactory.CreateScope().Returns(scope);
            scope.ServiceProvider.Returns(serviceProvider);
            serviceProvider.GetService(typeof(IUnitOfWorkManager)).Returns(uowManager);
            serviceProvider.GetService(typeof(IRepository<OutboxMessage, Guid>)).Returns(repository);
            serviceProvider.GetService(typeof(IOutboxMessageDispatcher)).Returns(dispatcher);
            uowManager.Begin(Arg.Any<AbpUnitOfWorkOptions>(), Arg.Any<bool>()).Returns(uow);

            var message = new OutboxMessage(
                Guid.NewGuid(),
                "TaskFinished",
                "{\"TaskId\":\"123\"}",
                "MES",
                "TRACE_TEST_01");

            repository.GetListAsync(
                Arg.Any<Expression<Func<OutboxMessage, bool>>>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
                .Returns(new List<OutboxMessage> { message });

            var worker = new OutboxPublisherWorker(scopeFactory, NullLogger<OutboxPublisherWorker>.Instance);

            // Act
            var count = await worker.ProcessBatchAsync(CancellationToken.None);

            // Assert
            count.ShouldBe(1);
            message.Status.ShouldBe(OutboxMessageStatus.Published);
            message.ProcessedTime.ShouldNotBeNull();
            await dispatcher.Received(1).DispatchAsync(message, Arg.Any<CancellationToken>());
            await repository.Received(1).UpdateAsync(message, autoSave: true, cancellationToken: Arg.Any<CancellationToken>());
            await uow.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
        }

        /// <summary>
        /// 测试在消息投递异常时，触发重试计数增加与指数退避时间计算
        /// </summary>
        [Fact]
        public async Task ProcessBatchAsync_Should_Handle_Dispatch_Failure_And_Record_Retry()
        {
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            var scope = Substitute.For<IServiceScope>();
            var serviceProvider = Substitute.For<IServiceProvider>();
            var uowManager = Substitute.For<IUnitOfWorkManager>();
            var uow = Substitute.For<IUnitOfWork>();
            var repository = Substitute.For<IRepository<OutboxMessage, Guid>>();
            var dispatcher = Substitute.For<IOutboxMessageDispatcher>();

            scopeFactory.CreateScope().Returns(scope);
            scope.ServiceProvider.Returns(serviceProvider);
            serviceProvider.GetService(typeof(IUnitOfWorkManager)).Returns(uowManager);
            serviceProvider.GetService(typeof(IRepository<OutboxMessage, Guid>)).Returns(repository);
            serviceProvider.GetService(typeof(IOutboxMessageDispatcher)).Returns(dispatcher);
            uowManager.Begin(Arg.Any<AbpUnitOfWorkOptions>(), Arg.Any<bool>()).Returns(uow);

            var message = new OutboxMessage(
                Guid.NewGuid(),
                "TaskFinished",
                "{\"TaskId\":\"123\"}",
                "MES",
                "TRACE_FAIL_01",
                maxRetries: 3);

            repository.GetListAsync(
                Arg.Any<Expression<Func<OutboxMessage, bool>>>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
                .Returns(new List<OutboxMessage> { message });

            // 模拟投递时网络异常抛出
            dispatcher.DispatchAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("Downstream HTTP 503 Service Unavailable"));

            var worker = new OutboxPublisherWorker(scopeFactory, NullLogger<OutboxPublisherWorker>.Instance);

            // Act
            var count = await worker.ProcessBatchAsync(CancellationToken.None);

            // Assert: 由于全部失败，处理成功计数应为 0
            count.ShouldBe(0);
            message.Status.ShouldBe(OutboxMessageStatus.Publishing);
            message.RetryCount.ShouldBe(1);
            message.LastError.ShouldNotBeNull();
            message.LastError.ShouldContain("503 Service Unavailable");
            message.NextRetryTime.ShouldNotBeNull();
            await repository.Received(1).UpdateAsync(message, autoSave: true, cancellationToken: Arg.Any<CancellationToken>());
            await uow.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
        }
    }
}
