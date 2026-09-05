using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Shouldly;
using SIASUN.RCS.Diagnostics.AI;
using SIASUN.RCS.Diagnostics.FlightPack;
using SIASUN.RCS.Infrastructure.Logging.Diagnostics.AI;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Tests.Diagnostics
{
    public class AiIncidentAnalysisProviderTests
    {
        private readonly FlightPackMetadata _sampleMetadata;
        private readonly List<FlightPackTimelineEvent> _sampleEvents;

        public AiIncidentAnalysisProviderTests()
        {
            _sampleMetadata = new FlightPackMetadata
            {
                PackVersion = "1.0.0",
                Anchor = new AnchorDto
                {
                    Type = "Task",
                    Key = "T-9999",
                    RelatedVehicleId = "AGV-01"
                },
                TimeWindow = new TimeWindowDto
                {
                    QueryStartTime = DateTime.UtcNow.AddMinutes(-10),
                    QueryEndTime = DateTime.UtcNow
                }
            };

            _sampleEvents = new List<FlightPackTimelineEvent>
            {
                new()
                {
                    Id = "1",
                    Timestamp = DateTime.UtcNow.AddMinutes(-5),
                    Track = "API",
                    Level = "Information",
                    Title = "POST /api/tasks (HTTP 200)",
                    Summary = "Task created"
                },
                new()
                {
                    Id = "2",
                    Timestamp = DateTime.UtcNow.AddMinutes(-2),
                    Track = "Exception",
                    Level = "Error",
                    Title = "DeadlockException",
                    Summary = "Deadlock at intersection 10"
                }
            };
        }

        [Fact]
        public async Task AnalyzeIncidentAsync_WhenDisabled_ShouldReturnFailedImmediatelyWithoutHttpCall()
        {
            // Arrange
            var options = Options.Create(new AiDiagnosticsOptions
            {
                IsEnabled = false
            });

            var handler = new MockHttpMessageHandler(_ =>
                throw new InvalidOperationException("HTTP call should not have been made when disabled!"));

            using var httpClient = new HttpClient(handler);
            var provider = new OpenAiCompatibleAiIncidentAnalysisProvider(
                httpClient: httpClient,
                options: options);

            // Act
            var result = await provider.AnalyzeIncidentAsync(_sampleMetadata, _sampleEvents, "Base narrative");

            // Assert
            result.ShouldNotBeNull();
            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldNotBeNull();
            result.ErrorMessage.ShouldContain("未启用");
        }

        [Fact]
        public async Task AnalyzeIncidentAsync_WhenSuccessResponse_ShouldParseStructuredSections()
        {
            // Arrange
            var options = Options.Create(new AiDiagnosticsOptions
            {
                IsEnabled = true,
                Endpoint = "http://mock-ai:11434/v1",
                Model = "deepseek-r1:7b"
            });

            var mockAiResponseText = @"【根因结论】：调度算法在十字路口CrossRoad-10与AGV-02产生路径互锁。
【责任归属】：调度算法/路径死锁
【置信度】：High
【推荐排障措施】：
1. 现场调度员将AGV-02切换至手动模式后移。
2. 调度算法配置中启用防死锁环路检测。
【详细时序逻辑分析】：
从API日志可见任务正常下发，但在02分系统抛出DeadlockException，说明路径规划未预估冲突。";

            var responseObj = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = mockAiResponseText
                        }
                    }
                }
            };

            var handler = new MockHttpMessageHandler(req =>
            {
                req.Method.ShouldBe(HttpMethod.Post);
                req.RequestUri!.ToString().ShouldEndWith("/chat/completions");

                var resp = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(responseObj))
                };
                return Task.FromResult(resp);
            });

            using var httpClient = new HttpClient(handler);
            var provider = new OpenAiCompatibleAiIncidentAnalysisProvider(
                httpClient: httpClient,
                options: options);

            // Act
            var result = await provider.AnalyzeIncidentAsync(_sampleMetadata, _sampleEvents, "Base narrative");

            // Assert
            result.ShouldNotBeNull();
            result.IsSuccess.ShouldBeTrue();
            result.ModelUsed.ShouldBe("deepseek-r1:7b");
            result.RootCauseSummary.ShouldContain("调度算法在十字路口CrossRoad-10");
            result.ResponsibleParty.ShouldBe("调度算法/路径死锁");
            result.ConfidenceLevel.ShouldBe("High");
            result.RecommendedActions.Count.ShouldBe(2);
            result.RecommendedActions[0].ShouldContain("手动模式后移");
            result.RecommendedActions[1].ShouldContain("启用防死锁环路检测");
            result.MarkdownReport.ShouldBe(mockAiResponseText);
        }

        [Fact]
        public async Task AnalyzeIncidentAsync_WhenHttpFails_ShouldGracefullyCatchAndReturnFailure()
        {
            // Arrange
            var options = Options.Create(new AiDiagnosticsOptions
            {
                IsEnabled = true,
                Endpoint = "http://mock-ai:11434/v1",
                Model = "deepseek-r1:7b"
            });

            var handler = new MockHttpMessageHandler(_ =>
            {
                var resp = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("Internal Model Error")
                };
                return Task.FromResult(resp);
            });

            using var httpClient = new HttpClient(handler);
            var provider = new OpenAiCompatibleAiIncidentAnalysisProvider(
                httpClient: httpClient,
                options: options);

            // Act
            var result = await provider.AnalyzeIncidentAsync(_sampleMetadata, _sampleEvents, "Base narrative");

            // Assert
            result.ShouldNotBeNull();
            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldNotBeNull();
            result.ErrorMessage.ShouldContain("AI 推理异常");
        }

        [Fact]
        public async Task AnalyzeIncidentAsync_WhenUnstructuredOutput_ShouldFallbackSafely()
        {
            // Arrange
            var options = Options.Create(new AiDiagnosticsOptions
            {
                IsEnabled = true,
                Endpoint = "http://mock-ai:11434/v1",
                Model = "deepseek-r1:7b"
            });

            var mockAiResponseText = "这是一个未经格式化的纯文本推理输出，没有包含标准方括号标签。";

            var responseObj = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = mockAiResponseText
                        }
                    }
                }
            };

            var handler = new MockHttpMessageHandler(_ =>
            {
                var resp = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(responseObj))
                };
                return Task.FromResult(resp);
            });

            using var httpClient = new HttpClient(handler);
            var provider = new OpenAiCompatibleAiIncidentAnalysisProvider(
                httpClient: httpClient,
                options: options);

            // Act
            var result = await provider.AnalyzeIncidentAsync(_sampleMetadata, _sampleEvents, "Base narrative");

            // Assert
            result.ShouldNotBeNull();
            result.IsSuccess.ShouldBeTrue();
            result.RootCauseSummary.ShouldContain("大模型未按固定段落输出根因");
            result.MarkdownReport.ShouldBe(mockAiResponseText);
        }

        private class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

            public MockHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
            {
                _handler = handler;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return _handler(request);
            }
        }
    }
}
