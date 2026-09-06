using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IO;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Infrastructure.Logging;
using SIASUN.RCS.Infrastructure.Logging.Diagnostics.SignalR;
using SIASUN.RCS.Infrastructure.Logging.Filtering;
using SIASUN.RCS.Infrastructure.Logging.OperationLogs;
using SIASUN.RCS.Logs.OperatorLogs;
using Volo.Abp.Security.Claims;
using Volo.Abp.Tracing;
using Volo.Abp.Users;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Tests.Diagnostics
{
    /// <summary>
    /// 全链路 TraceId/CorrelationId 绝对一致性集成测试
    /// 验证同一 HTTP 请求范围内，第 1 层 (API Audit)、第 2 层 (Operation Log) 与第 3 层 (Entity Audit) 的 TraceId 100% 锁死且字节对齐
    /// </summary>
    public class TraceIdAlignmentIntegrationTests
    {
        private readonly RecyclableMemoryStreamManager _streamManager = new();
        private readonly ApiAuditLogChannel _apiChannel = new();
        private readonly OperationLogChannelManager _opChannelManager = new();
        private readonly EntityAuditLogChannel _entityChannel = new();
        private readonly IAuditLogFilterEvaluator _filterEvaluator = Substitute.For<IAuditLogFilterEvaluator>();

        public TraceIdAlignmentIntegrationTests()
        {
            _filterEvaluator.ShouldAudit(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Direction>()).Returns(true);
        }

        [Fact]
        public async Task Request_With_Upstream_CorrelationId_Should_Align_Across_All_Three_Layers()
        {
            // Arrange
            const string upstreamTraceId = "MES-TRC-2026-9999-STRICT";
            var context = new DefaultHttpContext();
            context.Request.Method = "POST";
            context.Request.Path = "/api/v1/dispatch/create_task";
            context.Request.Headers["X-Correlation-Id"] = upstreamTraceId;
            var requestBody = "{\"taskId\":\"TASK-20260901\"}";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));

            var services = new ServiceCollection();
            var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            httpContextAccessor.HttpContext.Returns(context);
            services.AddSingleton(httpContextAccessor);

            var correlationIdProvider = Substitute.For<ICorrelationIdProvider>();
            correlationIdProvider.Get().Returns(upstreamTraceId);
            services.AddSingleton(correlationIdProvider);

            services.AddSingleton<IEntityAuditLogChannel>(_entityChannel);
            var serviceProvider = services.BuildServiceProvider();
            context.RequestServices = serviceProvider;

            var currentUser = Substitute.For<ICurrentUser>();
            currentUser.Id.Returns(Guid.NewGuid());
            currentUser.UserName.Returns("Dispatcher_01");

            var opRecorder = new OperationLogRecorder(
                _opChannelManager,
                currentUser,
                correlationIdProvider,
                liveStreamBroker: null,
                httpContextAccessor: httpContextAccessor
            );

            // 模拟下级管道中间件执行业务逻辑
            RequestDelegate next = async (ctx) =>
            {
                // 1. 业务逻辑记录第 2 层调度操作日志
                opRecorder.Record(new OperationLogContext
                {
                    Module = "Dispatch",
                    Action = "CreateTask",
                    TargetType = "Task",
                    TargetId = "TASK-20260901",
                    TaskId = "TASK-20260901",
                    BeforeState = null,
                    AfterState = "Pending",
                    Reason = "MES 生产指令下发"
                });

                // 2. 模拟第 3 层实体变更拦截
                var capturedTraceId = correlationIdProvider.Get()
                    ?? ctx.Items["__RcsCorrelationId"]?.ToString()
                    ?? ctx.Request.Headers["X-Correlation-Id"].ToString();

                _entityChannel.TryWrite(new EntityAuditLogMessage
                {
                    TraceId = capturedTraceId,
                    EntityName = "AgvTask",
                    EntityId = "TASK-20260901",
                    Action = "Added",
                    CreationTime = DateTime.UtcNow
                });

                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("{\"success\":true}");
            };

            var middleware = new InboundAuditMiddleware(next, _streamManager, _apiChannel, _filterEvaluator, null, correlationIdProvider);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _apiChannel.Reader.TryRead(out var apiLog).ShouldBeTrue();
            _opChannelManager.Channel.Reader.TryRead(out var opLog).ShouldBeTrue();
            _entityChannel.Reader.TryRead(out var entityLog).ShouldBeTrue();

            apiLog.ShouldNotBeNull();
            opLog.ShouldNotBeNull();
            entityLog.ShouldNotBeNull();

            // 核心铁律验证：三层日志 TraceId 必须 100% 完全相同并与上游一致
            apiLog.TraceId.ShouldBe(upstreamTraceId);
            opLog.CorrelationId.ShouldBe(upstreamTraceId);
            entityLog.TraceId.ShouldBe(upstreamTraceId);

            // 交叉一致性校验
            apiLog.TraceId.ShouldBe(opLog.CorrelationId);
            opLog.CorrelationId.ShouldBe(entityLog.TraceId);

            // 响应头回写验证
            context.Response.Headers["X-Correlation-Id"].ToString().ShouldBe(upstreamTraceId);
        }

        [Fact]
        public async Task Request_Without_Header_Should_Generate_Unified_TraceId_Across_All_Three_Layers()
        {
            // Arrange (无任何 TraceId 请求头，由 InboundAuditMiddleware 统一生成锚点)
            var context = new DefaultHttpContext();
            context.Request.Method = "POST";
            context.Request.Path = "/api/v1/dispatch/arrive";
            var requestBody = "{\"vehicleId\":\"AGV-01\"}";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));

            var services = new ServiceCollection();
            var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            httpContextAccessor.HttpContext.Returns(context);
            services.AddSingleton(httpContextAccessor);

            // 模拟 ABP CorrelationIdProvider 默认读取 Request.Headers["X-Correlation-Id"]
            var correlationIdProvider = Substitute.For<ICorrelationIdProvider>();
            correlationIdProvider.Get().Returns(callInfo => context.Request.Headers["X-Correlation-Id"].ToString());
            services.AddSingleton(correlationIdProvider);

            services.AddSingleton<IEntityAuditLogChannel>(_entityChannel);
            var serviceProvider = services.BuildServiceProvider();
            context.RequestServices = serviceProvider;

            var currentUser = Substitute.For<ICurrentUser>();
            currentUser.UserName.Returns("System");

            var opRecorder = new OperationLogRecorder(
                _opChannelManager,
                currentUser,
                correlationIdProvider,
                liveStreamBroker: null,
                httpContextAccessor: httpContextAccessor
            );

            RequestDelegate next = async (ctx) =>
            {
                opRecorder.Record(new OperationLogContext
                {
                    Module = "Fleet",
                    Action = "ReportArrival",
                    TargetType = "Vehicle",
                    TargetId = "AGV-01",
                    AgvId = "AGV-01",
                    Reason = "车辆物理到位心跳"
                });

                var capturedTraceId = correlationIdProvider.Get()
                    ?? ctx.Items["__RcsCorrelationId"]?.ToString()
                    ?? Guid.NewGuid().ToString("N");

                _entityChannel.TryWrite(new EntityAuditLogMessage
                {
                    TraceId = capturedTraceId,
                    EntityName = "VehicleStatus",
                    EntityId = "AGV-01",
                    Action = "Modified",
                    CreationTime = DateTime.UtcNow
                });

                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("{\"ok\":true}");
            };

            var middleware = new InboundAuditMiddleware(next, _streamManager, _apiChannel, _filterEvaluator, null, correlationIdProvider);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _apiChannel.Reader.TryRead(out var apiLog).ShouldBeTrue();
            _opChannelManager.Channel.Reader.TryRead(out var opLog).ShouldBeTrue();
            _entityChannel.Reader.TryRead(out var entityLog).ShouldBeTrue();

            apiLog.ShouldNotBeNull();
            opLog.ShouldNotBeNull();
            entityLog.ShouldNotBeNull();

            // 自动生成的 TraceId 不为空
            apiLog.TraceId.ShouldNotBeNullOrWhiteSpace();

            // 关键断言：即使请求未传入 TraceId，由中间件生成的 TraceId 在三层之间绝对一致
            apiLog.TraceId.ShouldBe(opLog.CorrelationId);
            opLog.CorrelationId.ShouldBe(entityLog.TraceId);

            // 且回写到 Response Headers
            context.Response.Headers["X-Correlation-Id"].ToString().ShouldBe(apiLog.TraceId);
        }
    }
}
