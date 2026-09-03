using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SIASUN.RCS.Infrastructure.Logging.OperationLogs;
using SIASUN.RCS.Logs.OperatorLog;
using SIASUN.RCS.Logs.OperatorLogs;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace SIASUN.RCS.Infrastructure.Tests.Logging.OperationLogs
{
    public class OperationLogPersistenceWorkerTests
    {
        [Fact]
        public async Task ExecuteAsync_Should_Read_From_Channel_And_Insert()
        {
            // Arrange
            var channelManager = new OperationLogChannelManager();
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            var logger = Substitute.For<ILogger<OperationLogPersistenceWorker>>();

            var scope = Substitute.For<IServiceScope>();
            var serviceProvider = Substitute.For<IServiceProvider>();
            var uowManager = Substitute.For<IUnitOfWorkManager>();
            var uow = Substitute.For<IUnitOfWork>();
            var repository = Substitute.For<IRepository<OperationLog, Guid>>();

            scopeFactory.CreateScope().Returns(scope);
            scope.ServiceProvider.Returns(serviceProvider);
            serviceProvider.GetService(typeof(IUnitOfWorkManager)).Returns(uowManager);
            serviceProvider.GetService(typeof(IRepository<OperationLog, Guid>)).Returns(repository);
            uowManager.Begin(Arg.Any<AbpUnitOfWorkOptions>(), Arg.Any<bool>()).Returns(uow);

            var worker = new OperationLogPersistenceWorker(channelManager, scopeFactory, logger);

            var log = new OperationLog(
                id: Guid.NewGuid(),
                operatorType: OperatorType.User,
                userId: Guid.NewGuid(),
                userName: "Test",
                clientIp: "",
                correlationId: "123",
                module: "M1",
                action: "A1",
                targetType: "T1",
                targetId: "K1",
                status: OperationLogStatus.Success,
                description: "Desc",
                errorMessage: null
            );

            // Push a log into the channel
            await channelManager.Channel.Writer.WriteAsync(log);

            // Act
            var cts = new CancellationTokenSource();
            var executeTask = worker.StartAsync(cts.Token);

            // Wait a little bit for the worker to process
            await Task.Delay(100);

            // Cancel to stop the worker loop
            cts.Cancel();
            await executeTask;

            // Assert
            await repository.Received(1).InsertAsync(log);
            await uow.Received(1).CompleteAsync();
            uow.Received(1).Dispose();
        }
    }
}
