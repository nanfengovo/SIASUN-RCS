using System.Text;
using Microsoft.AspNetCore.Http; // ← 解决 DefaultHttpContext / RequestDelegate
using Microsoft.IO;
using Shouldly;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Infrastructure.Logging;
using HttpMethod = SIASUN.RCS.Auditing.HttpMethod; // ← 解决 HttpMethod 命名冲突

namespace SIASUN.RCS.Infrastructure.Tests;

public class InboundAuditMiddlewareTests
{
    // 实例化被测中间件所需的依赖（纯内存对象，运行速度快）
    private readonly RecyclableMemoryStreamManager _streamManager = new();

    private readonly ApiAuditLogChannel _channel = new();


    /// <summary>
    /// 测试用例1:正常业务Post请求，中间件应该完整捕获Request/Repose 并推入Channel
    /// </summary>
    [Fact]
    public async Task InvokeAsync_NormalPostRequest_ShouldCapturePayloadAndPushToChannel()
    {
        //--------------- 1. Arrage(准备测试数据) ---------------
        var context = new DefaultHttpContext();
        context.Request.Method = "Post";
        context.Request.Path = "/api/v1/xinsong/task_arrive"; // 模拟新松TM接口
        var requestJson = "{\"task_serial\":\"TASK-1001\",\"action\":\"Arrive\"}";
        var requestBytes = Encoding.UTF8.GetBytes(requestJson);
        context.Request.Body = new MemoryStream(requestBytes);
        context.Request.ContentLength = requestBytes.Length;

        //模拟下游Controller 执行逻辑：返回200 OK 和响应 JSON
        RequestDelegate next = async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync("{\"Result\":true,\"ErrMsg\":\"\"}");
        };

        // 构建被测中间件实例
        var middleware = new InboundAuditMiddleware(next, _streamManager, _channel);

        // --------------- 2. Act(执行被测方法) -----------------
        await middleware.InvokeAsync(context);

        // ----------------- 3. Assert (断言验证结果) -----------------
        // 从 Channel 中取出捕获到的审计日志条目
        var readSuccess = _channel.Reader.TryRead(out var entry);
        readSuccess.ShouldBeTrue(); // 必须成功写入了 Channel
        entry.ShouldNotBeNull();
        // 验证各字段是否被准确解析
        entry.Direction.ShouldBe(Direction.Inbound);
        entry.Peer.ShouldBe("TM"); // 自动将 /xinsong/ 识别为 TM 对端系统
        entry.HttpMethod.ShouldBe(HttpMethod.Post);
        entry.Path.ShouldBe("/api/v1/xinsong/task_arrive");
        entry.StatusCode.ShouldBe(200);
        entry.RequestBody.ShouldBe(requestJson);
        entry.ResponseBody.ShouldBe("{\"Result\":true,\"ErrMsg\":\"\"}");
        entry.ElapsedMs.ShouldBeGreaterThanOrEqualTo(0); // 耗时必须大于等于 0 毫
    }



    /// <summary>
    /// [用例2: 空body查询流] 无请求体的GET请求，不应该报空指针异常切正常记录响应
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task InvokeAsync_GetRequestWithEmptyBody_ShouldRecordSuccessfully()
    {
        // 1. Arrange： 构造无Body的GET请求
        var context = new DefaultHttpContext();
        context.Request.Method = "Get";
        context.Request.Path = "/api/mes/query_task";
        context.Request.ContentLength = 0;

        RequestDelegate next = async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            await ctx.Response.WriteAsync("{\"status\":\"Running\"}");
        };

        var middleware = new InboundAuditMiddleware(next, _streamManager, _channel);

        //2. Act
        await middleware.InvokeAsync(context);

        //3. Assert
        _channel.Reader.TryRead(out var entry).ShouldBeTrue();

        entry.HttpMethod.ShouldBe(HttpMethod.Get);

        entry.RequestBody.ShouldBe(string.Empty);

        entry.ResponseBody.ShouldBe("{\"status\":\"Running\"}");
    }

    //【用例3: 白名单过滤】健康检查、SignaIR 握手与静态资源请求，必须静默放行，零日志产生
    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    [InlineData("/hubs/task-hub")]
    [InlineData("/dist/app.js")]
    [InlineData("/style/main.css")]
    [InlineData("/favico.ico")]
    public async Task InvokeAsync_WhitelistedPaths_ShouldBeIgnored(string path)
    {
        //1. Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        RequestDelegate next = (ctx) => Task.CompletedTask;

        var middleware = new InboundAuditMiddleware(next, _streamManager, _channel);

        //2.Act
        await middleware.InvokeAsync(context);

        //3. Assert: 验证Channel 为空，没有写入任何日志
        _channel.Reader.TryRead(out _).ShouldBeFalse();
    }



    //【用例 4：超大报文截断】超过 64KB 的请求体，应该被安全截断并追加 [TRUNCATED] 标识
    [Fact]
    public async Task InvokeAsync_OversizedRequestBody_ShouldBeTruncated()
    {
        // 1. Arrange: 构造70，000 字符的超大字符串
        var largePayload = new string('A', 70000);
        var context = new DefaultHttpContext();
        context.Request.Method = "Post";
        context.Request.Path = "/api/mes/upload_map";
        var bytes = Encoding.UTF8.GetBytes(largePayload);
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;

        RequestDelegate next = (ctx) => Task.CompletedTask;

        var middleware = new InboundAuditMiddleware(next, _streamManager, _channel);

        // 2. Art 
        await middleware.InvokeAsync(context);

        //3. Assert 
        _channel.Reader.TryRead(out var entry).ShouldBeTrue();
        entry.ShouldNotBeNull();
        entry.RequestBody!.Length.ShouldBeLessThan(70000);
        entry.RequestBody.Length.ShouldBe(65536 + " [TRUNCATED]".Length);
        entry.RequestBody.ShouldEndWith(" [TRUNCATED]");
    }

    //【用例 5：下游崩溃容错】Controller 抛出未捕获异常时，记录 500 状态码与错误信息，且原样向外抛出异常
    [Fact]
    public async Task InvokeAsync_WhenDownstreamThrows_ShouldRecord500AndRethrow()
    {
        // 1. Arrange: 下游抛出致命异常
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/v1/xinsong/task_error";
        RequestDelegate next = (ctx) => throw new InvalidOperationException("TM 车队调度连接断开！");
        var middleware = new InboundAuditMiddleware(next, _streamManager, _channel);
        // 2. Act & Assert: 验证中间件不吞异常，调用方能接住该异常
        var ex = await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await middleware.InvokeAsync(context);
        });
        ex.Message.ShouldBe("TM 车队调度连接断开！");
        // 3. Assert: 验证日志记录了 500 状态和异常详情
        _channel.Reader.TryRead(out var entry).ShouldBeTrue();
        entry.ShouldNotBeNull();
        entry.StatusCode.ShouldBe(500); // 异常时状态码置为 500
        entry.Exception.ShouldBe("TM 车队调度连接断开！");
    }

    //【用例 6：对端系统识别矩阵】根据 URL 自动精准识别 Peer 对端系统
    [Theory]
    [InlineData("/api/v1/xinsong/task_arrive", "TM")]
    [InlineData("/api/tm/dispatch", "TM")]
    [InlineData("/api/mes/order_create", "MES")]
    [InlineData("/api/gateway/unknown_device", "Unknown")]
    public async Task InvokeAsync_PeerResolutionMatrix_ShouldIdentifyCorrectPeer(string path, string expectedPeer)
    {
        // 1. Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = path;
        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new InboundAuditMiddleware(next, _streamManager, _channel);
        // 2. Act
        await middleware.InvokeAsync(context);
        // 3. Assert
        _channel.Reader.TryRead(out var entry).ShouldBeTrue();
        entry.ShouldNotBeNull();
        entry.Peer.ShouldBe(expectedPeer);
    }
}
