using System.Text;
using Microsoft.AspNetCore.Http; // ← 解决 DefaultHttpContext / RequestDelegate
using Microsoft.IO;
using Shouldly;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Infrastructure.Logging;
using Xunit;
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
}
