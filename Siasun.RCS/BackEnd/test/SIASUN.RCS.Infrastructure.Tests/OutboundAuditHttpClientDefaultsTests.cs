using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Infrastructure.Logging;
using SIASUN.RCS.Infrastructure.Logging.Filtering;
using Xunit;
using HttpMethod = SIASUN.RCS.Auditing.HttpMethod;

namespace SIASUN.RCS.Infrastructure.Tests
{
    /// <summary>
    /// 出站报文通过 ConfigureHttpClientDefaults 全局挂载到 IHttpClientFactory 管道的集成测试
    /// 验证任何通过工厂创建的 HttpClient 在不写一行额外代码的情况下，均能自动被 OutboundAuditDelegatingHandler 拦截
    /// </summary>
    public class OutboundAuditHttpClientDefaultsTests
    {
        private class MockHttpHandler : DelegatingHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

            public MockHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            {
                _handler = handler;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_handler(request));
            }
        }

        [Fact]
        public async Task HttpClientFactory_WithGlobalDefaults_ShouldAutomaticallyCaptureOutboundPackets()
        {
            // Arrange
            var services = new ServiceCollection();
            var channel = new ApiAuditLogChannel();
            var evaluator = Substitute.For<IAuditLogFilterEvaluator>();
            evaluator.ShouldAudit(Arg.Any<string>(), Arg.Any<string>(), Arg.Is(Direction.Outbound)).Returns(true);

            services.AddSingleton(channel);
            services.AddSingleton(evaluator);
            services.AddTransient<OutboundAuditDelegatingHandler>();

            // 全局配置默认管道
            services.ConfigureHttpClientDefaults(builder =>
            {
                builder.AddHttpMessageHandler<OutboundAuditDelegatingHandler>();
            });

            // 注册一个模拟的最底座 Handler（用于在测试中伪造对端响应）
            var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status\":\"ACK\",\"code\":0}", Encoding.UTF8, "application/json")
            };
            var bottomHandler = new MockHttpHandler(_ => mockResponse);

            // 注册测试用具名客户端，并插入 bottomHandler 作为 PrimaryHandler
            services.AddHttpClient("TMClient", client =>
            {
                client.BaseAddress = new Uri("http://127.0.0.1:8080");
            }).ConfigurePrimaryHttpMessageHandler(() => bottomHandler);

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IHttpClientFactory>();

            // Act: 业务层直接通过常规 HttpClientFactory 创建客户端并调用，完全无需手写任何日志拦截
            var client = factory.CreateClient("TMClient");
            var payload = new StringContent("{\"taskCode\":\"TASK-999\",\"action\":\"Fetch\"}", Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/v1/tm/dispatch", payload);

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            // 验证：底层 ApiAuditLogChannel 已经自动接收到了该出站报文
            channel.Reader.TryRead(out var entry).ShouldBeTrue();
            entry.ShouldNotBeNull();
            entry.Direction.ShouldBe(Direction.Outbound);
            entry.Peer.ShouldBe("TM");
            entry.HttpMethod.ShouldBe(HttpMethod.Post);
            entry.Path.ShouldBe("/api/v1/tm/dispatch");
            entry.StatusCode.ShouldBe(200);
            entry.RequestBody.ShouldBe("{\"taskCode\":\"TASK-999\",\"action\":\"Fetch\"}");
            entry.ResponseBody.ShouldBe("{\"status\":\"ACK\",\"code\":0}");
        }
    }
}

