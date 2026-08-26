using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.IO;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Infrastructure.Logging;
using SIASUN.RCS.Infrastructure.Logging.Filtering;
using Xunit;
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
    /// 【用例 6：对端系统识别矩阵】根据 URL 自动精准识别 Peer 对端系统
    /// </summary>
    [Theory]
    [InlineData("/api/v1/xinsong/task_arrive", "TM")]
    [InlineData("/api/tm/dispatch", "TM")]
    [InlineData("/api/mes/order_create", "MES")]
    [InlineData("/api/gateway/unknown_device", "Unknown")]
    public async Task InvokeAsync_PeerResolutionMatrix_ShouldIdentifyCorrectPeer(string path, string expectedPeer)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = path;
        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new InboundAuditMiddleware(next, _streamManager, _channel, _filterEvaluator);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _channel.Reader.TryRead(out var entry).ShouldBeTrue();
        entry.ShouldNotBeNull();
        entry.Peer.ShouldBe(expectedPeer);
    }
}
