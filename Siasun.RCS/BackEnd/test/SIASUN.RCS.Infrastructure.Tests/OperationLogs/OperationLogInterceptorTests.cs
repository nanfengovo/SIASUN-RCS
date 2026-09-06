using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Interfaces.OperationLogs;
using SIASUN.RCS.Infrastructure.Logging.OperationLogs;
using SIASUN.RCS.Logs.OperatorLog;
using SIASUN.RCS.Logs.OperatorLogs;
using Volo.Abp.DynamicProxy;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Tests.OperationLogs
{
    /// <summary>
    /// 声明式操作审计拦截器 AOP 单元测试
    /// </summary>
    public class OperationLogInterceptorTests
    {
        private class TestService
        {
            [OperationLog(Module = "TestModule", Action = "ExecuteAction", Description = "测试方法说明")]
            public virtual Task<string> AnnotatedMethodAsync(string taskId, string password)
            {
                return Task.FromResult($"Processed {taskId}");
            }

            [OperationLog(Module = "TestModule", Action = "FaultyAction")]
            public virtual Task ThrowingMethodAsync(string taskId)
            {
                throw new InvalidOperationException("Simulated service error");
            }

            public virtual Task PlainMethodAsync()
            {
                return Task.CompletedTask;
            }
        }

        private class MockMethodInvocation : IAbpMethodInvocation
        {
            public MethodInfo Method { get; set; } = null!;
            public object TargetObject { get; set; } = null!;
            public object[] Arguments { get; set; } = Array.Empty<object>();
            public IReadOnlyDictionary<string, object?> ArgumentsDictionary { get; set; } = new Dictionary<string, object?>();
            public Type[] GenericArguments { get; set; } = Array.Empty<Type>();
            public object ReturnValue { get; set; } = null!;

            private readonly Func<Task> _proceedFunc;

            public MockMethodInvocation(Func<Task> proceedFunc)
            {
                _proceedFunc = proceedFunc;
            }

            public Task ProceedAsync()
            {
                return _proceedFunc();
            }
        }

        [Fact]
        public async Task InterceptAsync_WhenAnnotatedMethodSucceeds_ShouldRecordSuccessAudit()
        {
            // Arrange
            var recorder = Substitute.For<IOperationLogRecorder>();
            var interceptor = new OperationLogInterceptor(recorder, NullLogger<OperationLogInterceptor>.Instance);

            var service = new TestService();
            var methodInfo = typeof(TestService).GetMethod(nameof(TestService.AnnotatedMethodAsync))!;

            MockMethodInvocation invocation = null!;
            invocation = new MockMethodInvocation(async () =>
            {
                await Task.Yield();
                invocation.ReturnValue = "Processed TASK-123";
            })
            {
                TargetObject = service,
                Method = methodInfo,
                ArgumentsDictionary = new Dictionary<string, object?>
                {
                    { "taskId", "TASK-123" },
                    { "password", "SuperSecret123" } // 敏感参数应被脱敏过滤
                }
            };

            // Act
            await interceptor.InterceptAsync(invocation);

            // Assert
            recorder.Received(1).Record(
                Arg.Is<OperationLogContext>(c =>
                    c.Module == "TestModule" &&
                    c.Action == "ExecuteAction" &&
                    c.TargetId == "TASK-123" &&
                    c.Description.Contains("TASK-123") &&
                    !c.Description.Contains("SuperSecret123") // 敏感密码被安全剔除
                ),
                Arg.Is(OperationLogStatus.Success),
                Arg.Is<string?>(s => s == null));
        }

        [Fact]
        public async Task InterceptAsync_WhenAnnotatedMethodThrows_ShouldRecordFailedAuditAndRethrow()
        {
            // Arrange
            var recorder = Substitute.For<IOperationLogRecorder>();
            var interceptor = new OperationLogInterceptor(recorder, NullLogger<OperationLogInterceptor>.Instance);

            var service = new TestService();
            var methodInfo = typeof(TestService).GetMethod(nameof(TestService.ThrowingMethodAsync))!;

            var invocation = new MockMethodInvocation(() => throw new InvalidOperationException("Simulated service error"))
            {
                TargetObject = service,
                Method = methodInfo,
                ArgumentsDictionary = new Dictionary<string, object?>
                {
                    { "taskId", "TASK-456" }
                }
            };

            // Act & Assert
            var ex = await Should.ThrowAsync<InvalidOperationException>(() => interceptor.InterceptAsync(invocation));
            ex.Message.ShouldBe("Simulated service error");

            recorder.Received(1).Record(
                Arg.Is<OperationLogContext>(c =>
                    c.Module == "TestModule" &&
                    c.Action == "FaultyAction" &&
                    c.TargetId == "TASK-456" &&
                    c.AfterState == "Failed"
                ),
                Arg.Is(OperationLogStatus.Failed),
                Arg.Is<string?>(msg => msg != null && msg.Contains("Simulated service error")));
        }

        [Fact]
        public async Task InterceptAsync_WhenMethodNotAnnotated_ShouldProceedWithoutRecording()
        {
            // Arrange
            var recorder = Substitute.For<IOperationLogRecorder>();
            var interceptor = new OperationLogInterceptor(recorder, NullLogger<OperationLogInterceptor>.Instance);

            var service = new TestService();
            var methodInfo = typeof(TestService).GetMethod(nameof(TestService.PlainMethodAsync))!;

            bool proceedCalled = false;
            var invocation = new MockMethodInvocation(() =>
            {
                proceedCalled = true;
                return Task.CompletedTask;
            })
            {
                TargetObject = service,
                Method = methodInfo
            };

            // Act
            await interceptor.InterceptAsync(invocation);

            // Assert
            proceedCalled.ShouldBeTrue();
            recorder.DidNotReceiveWithAnyArgs().Record(default!, default, default);
        }
    }
}
