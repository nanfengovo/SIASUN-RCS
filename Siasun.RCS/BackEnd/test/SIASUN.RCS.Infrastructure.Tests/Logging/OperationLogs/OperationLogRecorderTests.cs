using System;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Infrastructure.Logging.OperationLogs;
using SIASUN.RCS.Logs.OperatorLog;
using SIASUN.RCS.Logs.OperatorLogs;
using Volo.Abp.Tracing;
using Volo.Abp.Users;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Tests.Logging.OperationLogs
{
    public class OperationLogRecorderTests
    {
        private readonly OperationLogChannelManager _channelManager;
        private readonly ICurrentUser _currentUser;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly OperationLogRecorder _recorder;

        public OperationLogRecorderTests()
        {
            _channelManager = new OperationLogChannelManager();
            _currentUser = Substitute.For<ICurrentUser>();
            _correlationIdProvider = Substitute.For<ICorrelationIdProvider>();

            _recorder = new OperationLogRecorder(_channelManager, _currentUser, _correlationIdProvider);
        }

        [Fact]
        public async Task RecordSuccess_Should_Write_To_Channel()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _currentUser.Id.Returns(userId);
            _currentUser.UserName.Returns("TestUser");
            _correlationIdProvider.Get().Returns("TestCorrelationId");

            var module = "TestModule";
            var action = "TestAction";
            var targetType = "Task";
            var targetKey = "T-001";
            var description = "Test success description";

            // Act
            _recorder.RecordSuccess(module, action, targetType, targetKey, description);

            // Assert
            var reader = _channelManager.Channel.Reader;
            reader.Count.ShouldBe(1);

            var log = await reader.ReadAsync();
            log.ShouldNotBeNull();
            log.Module.ShouldBe(module);
            log.Action.ShouldBe(action);
            log.TargetType.ShouldBe(targetType);
            log.TargetId.ShouldBe(targetKey);
            log.Description.ShouldBe(description);
            log.Status.ShouldBe(OperationLogStatus.Success);
            log.ErrorMessage.ShouldBeNull();
            log.UserId.ShouldBe(userId);
            log.UserName.ShouldBe("TestUser");
            log.OperatorType.ShouldBe(OperatorType.User);
            log.CorrelationId.ShouldBe("TestCorrelationId");
        }

        [Fact]
        public async Task RecordFailure_Should_Write_To_Channel_With_System_Operator()
        {
            // Arrange
            // Simulate a system user (no UserId)
            _currentUser.Id.Returns((Guid?)null);
            _currentUser.UserName.Returns((string)null);
            _correlationIdProvider.Get().Returns("SysCorrelationId");

            var module = "SystemModule";
            var action = "SystemAction";
            var targetType = "Config";
            var targetKey = "C-123";
            var description = "Test failure description";
            var errorMsg = "System Error";

            // Act
            _recorder.RecordFailure(module, action, targetType, targetKey, description, errorMsg);

            // Assert
            var reader = _channelManager.Channel.Reader;
            reader.Count.ShouldBe(1);

            var log = await reader.ReadAsync();
            log.ShouldNotBeNull();
            log.Module.ShouldBe(module);
            log.Status.ShouldBe(OperationLogStatus.Failed);
            log.ErrorMessage.ShouldBe(errorMsg);
            log.OperatorType.ShouldBe(OperatorType.System);
            log.UserName.ShouldBe("System"); // Default fallback
            log.CorrelationId.ShouldBe("SysCorrelationId");
        }
    }
}
