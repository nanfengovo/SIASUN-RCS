using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.IO;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Infrastructure.Logging;
using SIASUN.RCS.Infrastructure.Logging.Filtering;
using HttpMethod = SIASUN.RCS.Auditing.HttpMethod;

namespace SIASUN.RCS.Infrastructure.Tests;

public class InboundAuditMiddlewareTests
{
    private readonly RecyclableMemoryStreamManager _streamManager = new();
    private readonly ApiAuditLogChannel _channel = new();
    private readonly IAuditLogFilterEvaluator _filterEvaluator = Substitute.For<IAuditLogFilterEvaluator>();

    public InboundAuditMiddlewareTests()
    {
        // 默认让 evaluator 对所有 /api/* 路径放行允许记录
        _filterEvaluator.ShouldAudit(
            Arg.Is<string>(p => p.StartsWith("/api/")),
            Arg.Any<string>(),
            Arg.Any<Direction>()).Returns(true);
    }

    /// <summary>
    /// 测试用例1:正常业务Post请求，中间件应该完整捕获Request/Repose 并推入Channel
    /// </summary>
    [Fact]
    public async Task InvokeAsync_NormalPostRequest_ShouldCapturePayloadAndPushToChannel()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "Post";
        context.Request.Path = "/api/v1/xinsong/task_arrive";
        var requestJson = "{\"task_serial\":\"TASK-1001\",\"action\":\"Arrive\"}";
        var requestBytes = Encoding.UTF8.GetBytes(requestJson);
        context.Request.Body = new MemoryStream(requestBytes);
        context.Request.ContentLength = requestBytes.Length;

        // 模拟 Endpoint 上的 AuditPeer 特性
        var metadata = new Microsoft.AspNetCore.Http.EndpointMetadataCollection(new AuditPeerAttribute("TM"));
        var endpoint = new Microsoft.AspNetCore.Http.Endpoint(c => Task.CompletedTask, metadata, "TestEndpoint");
        context.SetEndpoint(endpoint);

        RequestDelegate next = async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync("{\"Result\":true,\"ErrMsg\":\"\"}");
        };

        var middleware = new InboundAuditMiddleware(next, _streamManager, _channel, _filterEvaluator);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var readSuccess = _channel.Reader.TryRead(out var entry);
        readSuccess.ShouldBeTrue();
        entry.ShouldNotBeNull();
        entry.Direction.ShouldBe(Direction.Inbound);
        entry.Peer.ShouldBe("TM");
        entry.HttpMethod.ShouldBe(HttpMethod.Post);
        entry.Path.ShouldBe("/api/v1/xinsong/task_arrive");
        entry.StatusCode.ShouldBe(200);
        entry.RequestBody.ShouldBe(requestJson);
        entry.ResponseBody.ShouldBe("{\"Result\":true,\"ErrMsg\":\"\"}");
        entry.ElapsedMs.ShouldBeGreaterThanOrEqualTo(0);
    }

    /// <summary>
    /// [用例2: 空body查询流] 无请求体的GET请求，不应该报空指针异常且正常记录响应
    /// </summary>
    [Fact]
    public async Task InvokeAsync_GetRequestWithEmptyBody_ShouldRecordSuccessfully()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "Get";
        context.Request.Path = "/api/mes/query_task";
        context.Request.ContentLength = 0;

        RequestDelegate next = async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            await ctx.Response.WriteAsync("{\"status\":\"Running\"}");
        };

        var middleware = new InboundAuditMiddleware(next, _streamManager, _channel, _filterEvaluator);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _channel.Reader.TryRead(out var entry).ShouldBeTrue();
        entry.HttpMethod.ShouldBe(HttpMethod.Get);
        entry.RequestBody.ShouldBe(string.Empty);
        entry.ResponseBody.ShouldBe("{\"status\":\"Running\"}");
    }

    /// <summary>
    /// 【用例3: 过滤判定】当 evaluator.ShouldAudit 返回 false 时，中间件必须静默放行，零日志产生
    /// </summary>
    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    [InlineData("/hubs/task-hub")]
    [InlineData("/dist/app.js")]
    [InlineData("/style/main.css")]
    [InlineData("/favico.ico")]
    public async Task InvokeAsync_WhenFilterRejects_ShouldBeIgnored(string path)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        RequestDelegate next = (ctx) => Task.CompletedTask;

        _filterEvaluator.ShouldAudit(path, Arg.Any<string>(), Arg.Any<Direction>()).Returns(false);

        var middleware = new InboundAuditMiddleware(next, _streamManager, _channel, _filterEvaluator);

        // Act
        await middleware.InvokeAsync(context);

        // Assert: 验证 Channel 为空
        _channel.Reader.TryRead(out _).ShouldBeFalse();
    }

    /// <summary>
    /// 【用例 4：超大报文截断】超过 64KB 的请求体，应该被安全截断并追加 [TRUNCATED] 标识
    /// </summary>
    [Fact]
    public async Task InvokeAsync_OversizedRequestBody_ShouldBeTruncated()
    {
        // Arrange: 构造 70,000 字符的超大字符串
        var largePayload = new string('A', 70000);
        var context = new DefaultHttpContext();
        context.Request.Method = "Post";
        context.Request.Path = "/api/mes/upload_map";
        var bytes = Encoding.UTF8.GetBytes(largePayload);
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;

        RequestDelegate next = (ctx) => Task.CompletedTask;

        var middleware = new InboundAuditMiddleware(next, _streamManager, _channel, _filterEvaluator);

        // Act 
        await middleware.InvokeAsync(context);

        // Assert 
        _channel.Reader.TryRead(out var entry).ShouldBeTrue();
        entry.ShouldNotBeNull();
        entry.RequestBody!.Length.ShouldBeLessThan(70000);
        entry.RequestBody.Length.ShouldBe(65536 + " [TRUNCATED]".Length);
        entry.RequestBody.ShouldEndWith(" [TRUNCATED]");
    }

    /// <summary>
    /// 【用例 5：下游崩溃容错】Controller 抛出未捕获异常时，记录 500 状态码与错误信息，且原样向外抛出异常
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WhenDownstreamThrows_ShouldRecord500AndRethrow()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/v1/xinsong/task_error";
        RequestDelegate next = (ctx) => throw new InvalidOperationException("TM 车队调度连接断开！");
        var middleware = new InboundAuditMiddleware(next, _streamManager, _channel, _filterEvaluator);

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await middleware.InvokeAsync(context);
        });
        ex.Message.ShouldBe("TM 车队调度连接断开！");

        _channel.Reader.TryRead(out var entry).ShouldBeTrue();
        entry.ShouldNotBeNull();
        entry.StatusCode.ShouldBe(500);
        entry.Exception.ShouldBe("TM 车队调度连接断开！");
    }

    /// <summary>
    /// 【用例 6：对端系统识别矩阵】根据 Endpoint 上的特性精准识别 Peer 对端系统，无特性则回退到 "Unknown"
    /// </summary>
    [Theory]
    [InlineData("TM", "TM")]
    [InlineData("MES", "MES")]
    [InlineData(null, "Unknown")]
    public async Task InvokeAsync_PeerResolution_ShouldIdentifyCorrectPeerFromEndpointAttribute(string? attributePeerName, string expectedPeer)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/test";

        if (attributePeerName != null)
        {
            var metadata = new Microsoft.AspNetCore.Http.EndpointMetadataCollection(new AuditPeerAttribute(attributePeerName));
            var endpoint = new Microsoft.AspNetCore.Http.Endpoint(c => Task.CompletedTask, metadata, "TestEndpoint");
            context.SetEndpoint(endpoint);
        }

        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new InboundAuditMiddleware(next, _streamManager, _channel, _filterEvaluator);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _channel.Reader.TryRead(out var entry).ShouldBeTrue();
        entry.ShouldNotBeNull();
        entry.Peer.ShouldBe(expectedPeer);
    }

    /// <summary>
    /// 【用例 7：L4 洪峰自适应限流】在突发洪峰或高频请求风暴时，非特权常规 200 OK 请求被降采样过滤保盘，而 401 拒录 / 500 崩溃 / 调度干预 100% 绝对入库
    /// </summary>
    [Fact]
    public async Task InvokeAsync_Under_CriticalBurst_Should_Throttle_Non_Privileged_Success_Api_While_Preserving_Errors_And_Dispatch()
    {
        var governor = Substitute.For<SIASUN.RCS.Diagnostics.IAdaptiveTrafficGovernor>();
        // 模拟限流策略：非特权常规 200 请求丢弃，异常 401/500 与调度特权请求准入
        governor.ShouldAdmit("Unknown", "Information")
            .Returns(SIASUN.RCS.Diagnostics.TrafficSamplingDecision.Drop(SIASUN.RCS.Diagnostics.TrafficGovernorLevel.CriticalBurst, 0.1, "Storm"));
        governor.ShouldAdmit(Arg.Any<string>(), "Error")
            .Returns(SIASUN.RCS.Diagnostics.TrafficSamplingDecision.Admit(SIASUN.RCS.Diagnostics.TrafficGovernorLevel.CriticalBurst));
        governor.ShouldAdmit(Arg.Any<string>(), "Warning")
            .Returns(SIASUN.RCS.Diagnostics.TrafficSamplingDecision.Admit(SIASUN.RCS.Diagnostics.TrafficGovernorLevel.CriticalBurst));
        governor.ShouldAdmit("Dispatch", "Information")
            .Returns(SIASUN.RCS.Diagnostics.TrafficSamplingDecision.Admit(SIASUN.RCS.Diagnostics.TrafficGovernorLevel.CriticalBurst));

        // 1. 发起一个常规 200 OK 请求 -> 应该被限流丢弃，不推入 Channel
        var normalContext = new DefaultHttpContext();
        normalContext.Request.Method = "GET";
        normalContext.Request.Path = "/api/v1/ping";
        RequestDelegate nextNormal = (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        };
        var middlewareNormal = new InboundAuditMiddleware(nextNormal, _streamManager, _channel, _filterEvaluator, null, null, governor);
        await middlewareNormal.InvokeAsync(normalContext);
        _channel.Reader.TryRead(out _).ShouldBeFalse(); // 证实被降采样丢弃

        // 2. 发起一个 401 Unauthorized 认证失败请求 -> 核心铁证，必须 100% 写入
        var authFailContext = new DefaultHttpContext();
        authFailContext.Request.Method = "POST";
        authFailContext.Request.Path = "/api/v1/dispatch/create";
        RequestDelegate nextAuthFail = (ctx) =>
        {
            ctx.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
        var middlewareAuth = new InboundAuditMiddleware(nextAuthFail, _streamManager, _channel, _filterEvaluator, null, null, governor);
        await middlewareAuth.InvokeAsync(authFailContext);
        _channel.Reader.TryRead(out var authEntry).ShouldBeTrue();
        authEntry.ShouldNotBeNull();
        authEntry.StatusCode.ShouldBe(401);

        // 3. 发起一个 Dispatch 核心调度请求（状态码 200） -> 调度特权，必须 100% 写入
        var dispatchContext = new DefaultHttpContext();
        dispatchContext.Request.Method = "POST";
        dispatchContext.Request.Path = "/api/v1/dispatch/cancel";
        var metadata = new Microsoft.AspNetCore.Http.EndpointMetadataCollection(new AuditPeerAttribute("Dispatch"));
        var endpoint = new Microsoft.AspNetCore.Http.Endpoint(c => Task.CompletedTask, metadata, "DispatchEndpoint");
        dispatchContext.SetEndpoint(endpoint);
        RequestDelegate nextDispatch = (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        };
        var middlewareDispatch = new InboundAuditMiddleware(nextDispatch, _streamManager, _channel, _filterEvaluator, null, null, governor);
        await middlewareDispatch.InvokeAsync(dispatchContext);
        _channel.Reader.TryRead(out var dispatchEntry).ShouldBeTrue();
        dispatchEntry.ShouldNotBeNull();
        dispatchEntry.Peer.ShouldBe("Dispatch");
        dispatchEntry.StatusCode.ShouldBe(200);
    }
}
