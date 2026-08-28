using System.Net;
using System.Text;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Infrastructure.Logging;
using SIASUN.RCS.Infrastructure.Logging.Filtering;
using HttpMethod = SIASUN.RCS.Auditing.HttpMethod;

namespace SIASUN.RCS.Infrastructure.Tests;

public class OutboundAuditDelegatingHandlerTests
{
    private readonly ApiAuditLogChannel _channel = new();
    private readonly IAuditLogFilterEvaluator _filterEvaluator = Substitute.For<IAuditLogFilterEvaluator>();

    public OutboundAuditDelegatingHandlerTests()
    {
        // 默认让 evaluator 对 Outbound 全部放行
        _filterEvaluator.ShouldAudit(Arg.Any<string>(), Arg.Any<string>(), Arg.Is(Direction.Outbound)).Returns(true);
    }

    /// <summary>
    /// 模拟的内层 Handler，用于拦截真实的 HTTP 网络请求，直接在内存返回假数据
    /// </summary>
    private class TestInnerHandler : DelegatingHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public TestInnerHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    /// <summary>
    /// 【用例1：正常出站请求】应完整捕获请求体与响应体并推入 Channel
    /// </summary>
    [Fact]
    public async Task SendAsync_NormalPostRequest_ShouldCaptureAndPushToChannel()
    {
        // Arrange
        var innerHandler = new TestInnerHandler(req =>
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"result\": true}", Encoding.UTF8, "application/json")
            };
        });

        var handler = new OutboundAuditDelegatingHandler(_channel, _filterEvaluator)
        {
            InnerHandler = innerHandler
        };

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };

        var content = new StringContent("{\"action\":\"start\"}", Encoding.UTF8, "application/json");

        // Act
        await client.PostAsync("/api/v1/tm/dispatch", content);

        // Assert
        var readSuccess = _channel.Reader.TryRead(out var entry);
        readSuccess.ShouldBeTrue();
        entry.ShouldNotBeNull();
        entry.Direction.ShouldBe(Direction.Outbound);
        entry.Peer.ShouldBe("TM");
        entry.HttpMethod.ShouldBe(HttpMethod.Post);
        entry.Path.ShouldBe("/api/v1/tm/dispatch"); // 此处之前帮您修过，确保是 AbsolutePath
        entry.StatusCode.ShouldBe(200);
        entry.RequestBody.ShouldBe("{\"action\":\"start\"}");
        entry.ResponseBody.ShouldBe("{\"result\": true}");
        entry.ElapsedMs.ShouldBeGreaterThanOrEqualTo(0);
    }

    /// <summary>
    /// 【用例2：过滤判定】当 evaluator 拒绝记录时，必须直接放行，不产生任何日志
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenFilterRejects_ShouldNotCapture()
    {
        // Arrange
        _filterEvaluator.ShouldAudit(Arg.Any<string>(), Arg.Any<string>(), Arg.Is(Direction.Outbound)).Returns(false);

        var innerHandler = new TestInnerHandler(req => new HttpResponseMessage(HttpStatusCode.OK));
        var handler = new OutboundAuditDelegatingHandler(_channel, _filterEvaluator)
        {
            InnerHandler = innerHandler
        };
        var client = new HttpClient(handler);

        // Act
        await client.GetAsync("http://localhost:8080/health");

        // Assert
        _channel.Reader.TryRead(out _).ShouldBeFalse();
    }

    /// <summary>
    /// 【用例3：下游连接异常容错】出站连接失败或抛出异常时，应记录 500 状态码并原样抛出异常
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenDownstreamThrows_ShouldRecord500AndRethrow()
    {
        // Arrange
        var innerHandler = new TestInnerHandler(req => throw new HttpRequestException("Connection Refused"));
        var handler = new OutboundAuditDelegatingHandler(_channel, _filterEvaluator)
        {
            InnerHandler = innerHandler
        };
        var client = new HttpClient(handler);

        // Act & Assert
        var ex = await Should.ThrowAsync<HttpRequestException>(async () =>
        {
            await client.GetAsync("http://mes-system.local/api/mes/sync");
        });

        ex.Message.ShouldBe("Connection Refused");

        _channel.Reader.TryRead(out var entry).ShouldBeTrue();
        entry.ShouldNotBeNull();
        entry.StatusCode.ShouldBe(500);
        entry.Exception.ShouldBe("Connection Refused");
        entry.Peer.ShouldBe("MES");
    }

    /// <summary>
    /// 【用例4：超大响应报文截断】响应体超过 64KB 时，应安全截断并追加 [TRUNCATED]
    /// </summary>
    [Fact]
    public async Task SendAsync_OversizedResponseBody_ShouldBeTruncated()
    {
        // Arrange
        var largeResponse = new string('B', 70000);
        var innerHandler = new TestInnerHandler(req =>
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(largeResponse, Encoding.UTF8, "text/plain")
            };
        });

        var handler = new OutboundAuditDelegatingHandler(_channel, _filterEvaluator)
        {
            InnerHandler = innerHandler
        };

        var client = new HttpClient(handler);

        // Act
        await client.GetAsync("http://tm-server/api/tm/map");

        // Assert
        _channel.Reader.TryRead(out var entry).ShouldBeTrue();
        entry.ShouldNotBeNull();
        entry.ResponseBody!.Length.ShouldBe(65536 + " [TRUNCATED]".Length);
        entry.ResponseBody.ShouldEndWith(" [TRUNCATED]");
    }
}

