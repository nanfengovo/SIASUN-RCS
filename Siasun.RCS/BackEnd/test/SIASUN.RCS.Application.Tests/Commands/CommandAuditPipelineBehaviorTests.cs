using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Commands;
using SIASUN.RCS.Interfaces.OperationLogs;
using SIASUN.RCS.Logs.OperatorLog;
using SIASUN.RCS.Logs.OperatorLogs;
using Xunit;

namespace SIASUN.RCS.Application.Tests.Commands
{
    /// <summary>
    /// CQRS 全局命令自动审计管道行为单元测试
    /// 验证管道对业务命令的拦截、耗时测量、异常捕获以及零手动日志自动化记录特性
    /// </summary>
    public class CommandAuditPipelineBehaviorTests
    {
        private readonly IOperationLogRecorder _opRecorder;
        private readonly ILogger<CommandAuditPipelineBehavior<TestAuditableCommand, TestCommandResponse>> _logger;

        public CommandAuditPipelineBehaviorTests()
        {
            _opRecorder = Substitute.For<IOperationLogRecorder>();
            _logger = Substitute.For<ILogger<CommandAuditPipelineBehavior<TestAuditableCommand, TestCommandResponse>>>();
        }

        #region 测试用模拟命令与响应

        public record TestAuditableCommand(string Id, string Reason) : ICommand<TestCommandResponse>, IAuditableCommand
        {
            public string Module => "TestModule";
            public string Action => "TestAction";
            public string TargetType => "TestTarget";
            public string? TargetId => Id;
            public string? Description => $"测试动作 [{Id}]";
        }

        public record TestNonAuditableRequest : IRequest<string>;

        public record TestPlainCommand : ICommand<string>;

        public class TestCommandResponse : IStateTransitionResult
        {
            public bool Success { get; set; } = true;
            public string? BeforeState { get; set; }
            public string? AfterState { get; set; }
            public string? TargetType { get; set; }
            public string? TargetId { get; set; }
        }

        #endregion

        [Fact]
        public async Task Handle_WhenAuditableCommandSucceeds_Should_RecordSuccessOperationLog_WithTransitions()
        {
            // Arrange
            var behavior = new CommandAuditPipelineBehavior<TestAuditableCommand, TestCommandResponse>(_opRecorder, _logger);
            var cmd = new TestAuditableCommand("T-001", "测试正常推进");

            var expectedResponse = new TestCommandResponse
            {
                BeforeState = "Pending",
                AfterState = "Running",
                TargetType = "TestTarget",
                TargetId = "T-001"
            };

            RequestHandlerDelegate<TestCommandResponse> next = (ct) => Task.FromResult(expectedResponse);

            // Act
            var response = await behavior.Handle(cmd, next, CancellationToken.None);

            // Assert
            response.ShouldBe(expectedResponse);

            _opRecorder.Received(1).Record(Arg.Is<OperationLogContext>(ctx =>
                ctx.Module == "TestModule" &&
                ctx.Action == "TestAction" &&
                ctx.TargetType == "TestTarget" &&
                ctx.TargetId == "T-001" &&
                ctx.BeforeState == "Pending" &&
                ctx.AfterState == "Running" &&
                ctx.Reason == "测试正常推进" &&
                ctx.Description.Contains("测试动作 [T-001]") &&
                ctx.Description.Contains("耗时:")
            ), OperationLogStatus.Success, null);
        }

        [Fact]
        public async Task Handle_WhenAuditableCommandThrows_Should_RecordFailedOperationLog_And_Rethrow()
        {
            // Arrange
            var behavior = new CommandAuditPipelineBehavior<TestAuditableCommand, TestCommandResponse>(_opRecorder, _logger);
            var cmd = new TestAuditableCommand("T-002", "异常测试");

            RequestHandlerDelegate<TestCommandResponse> next = (ct) => throw new InvalidOperationException("模拟硬件故障");

            // Act & Assert
            var ex = await Should.ThrowAsync<InvalidOperationException>(async () =>
            {
                await behavior.Handle(cmd, next, CancellationToken.None);
            });
            ex.Message.ShouldBe("模拟硬件故障");

            _opRecorder.Received(1).Record(Arg.Is<OperationLogContext>(ctx =>
                ctx.Module == "TestModule" &&
                ctx.Action == "TestAction" &&
                ctx.TargetId == "T-002" &&
                ctx.BeforeState == null &&
                ctx.AfterState == null &&
                ctx.Reason == "异常测试" &&
                ctx.Description.Contains("失败：模拟硬件故障")
            ), OperationLogStatus.Failed, "模拟硬件故障");
        }

        [Fact]
        public async Task Handle_WhenRequestIsNotCommandOrAuditable_Should_BypassAuditing()
        {
            // Arrange
            var nonAuditableLogger = Substitute.For<ILogger<CommandAuditPipelineBehavior<TestNonAuditableRequest, string>>>();
            var behavior = new CommandAuditPipelineBehavior<TestNonAuditableRequest, string>(_opRecorder, nonAuditableLogger);
            var req = new TestNonAuditableRequest();

            RequestHandlerDelegate<string> next = (ct) => Task.FromResult("PassThrough");

            // Act
            var result = await behavior.Handle(req, next, CancellationToken.None);

            // Assert
            result.ShouldBe("PassThrough");
            // 纯查询或非审计请求绝不打扰 OperationLogRecorder
            _opRecorder.DidNotReceive().Record(Arg.Any<OperationLogContext>(), Arg.Any<OperationLogStatus>(), Arg.Any<string?>());
        }

        [Fact]
        public async Task Handle_WhenPlainCommandWithoutAuditableMetadata_Should_FallbackToTypeNameReflection()
        {
            // Arrange
            var plainLogger = Substitute.For<ILogger<CommandAuditPipelineBehavior<TestPlainCommand, string>>>();
            var behavior = new CommandAuditPipelineBehavior<TestPlainCommand, string>(_opRecorder, plainLogger);
            var cmd = new TestPlainCommand();

            RequestHandlerDelegate<string> next = (ct) => Task.FromResult("OK");

            // Act
            var result = await behavior.Handle(cmd, next, CancellationToken.None);

            // Assert
            result.ShouldBe("OK");

            _opRecorder.Received(1).Record(Arg.Is<OperationLogContext>(ctx =>
                ctx.Module == "Application" &&
                ctx.Action == "TestPlain" && // 去掉 Command 后缀
                ctx.TargetType == "Command"
            ), OperationLogStatus.Success, null);
        }
    }
}
