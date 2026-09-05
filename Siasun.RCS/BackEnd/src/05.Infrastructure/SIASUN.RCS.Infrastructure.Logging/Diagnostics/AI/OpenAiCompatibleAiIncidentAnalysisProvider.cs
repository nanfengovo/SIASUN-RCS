using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIASUN.RCS.Diagnostics.AI;
using SIASUN.RCS.Diagnostics.FlightPack;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Infrastructure.Logging.Diagnostics.AI
{
    public class OpenAiCompatibleAiIncidentAnalysisProvider : IAiIncidentAnalysisProvider, ITransientDependency
    {
        private readonly IHttpClientFactory? _httpClientFactory;
        private readonly HttpClient? _directHttpClient;
        private readonly AiDiagnosticsOptions _options;
        private readonly ILogger<OpenAiCompatibleAiIncidentAnalysisProvider> _logger;

        public bool IsEnabled => _options.IsEnabled;

        public OpenAiCompatibleAiIncidentAnalysisProvider(
            IHttpClientFactory? httpClientFactory = null,
            HttpClient? httpClient = null,
            IOptions<AiDiagnosticsOptions>? options = null,
            ILogger<OpenAiCompatibleAiIncidentAnalysisProvider>? logger = null)
        {
            _httpClientFactory = httpClientFactory;
            _directHttpClient = httpClient;
            _options = options?.Value ?? new AiDiagnosticsOptions();
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<OpenAiCompatibleAiIncidentAnalysisProvider>.Instance;
        }

        private HttpClient GetClient()
        {
            if (_directHttpClient != null)
            {
                return _directHttpClient;
            }

            if (_httpClientFactory != null)
            {
                return _httpClientFactory.CreateClient("AiDiagnostics");
            }

            return new HttpClient();
        }

        public async Task<AiAnalysisResultDto> AnalyzeIncidentAsync(
            FlightPackMetadata metadata,
            IReadOnlyList<FlightPackTimelineEvent> events,
            string baseNarrative,
            CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();

            if (!_options.IsEnabled)
            {
                return new AiAnalysisResultDto
                {
                    IsSuccess = false,
                    ErrorMessage = "AI 诊断模块在系统配置中未启用 (AiDiagnostics:IsEnabled = false)"
                };
            }

            try
            {
                var prompt = BuildPrompt(metadata, events, baseNarrative);
                var requestBody = new
                {
                    model = _options.Model,
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = "你是一位工业级移动机器人（AGV/AMR）调度控制系统（SIASUN RCS）高级故障诊断专家。" +
                                      "请根据提供的黑匣子全时序事件与基础因果叙事，进行严谨的根因推断并输出排障报告。\n" +
                                      "必须明确输出以下四个关键模块：\n" +
                                      "【根因结论】：一句话总结导致故障的核心原因\n" +
                                      "【责任归属】：调度算法/路径死锁 | 现场误操作/人工干预 | 硬件通信/车体故障 | 上游对接/下发异常\n" +
                                      "【置信度】：High | Medium | Low\n" +
                                      "【推荐排障措施】：按优先级列出现场实施/运维处置步骤\n" +
                                      "【详细时序逻辑分析】：结合多米诺骨牌时序进行详细推演"
                        },
                        new
                        {
                            role = "user",
                            content = prompt
                        }
                    },
                    max_tokens = _options.MaxTokens,
                    temperature = _options.Temperature
                };

                var endpointUrl = _options.Endpoint.TrimEnd('/');
                if (!endpointUrl.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
                {
                    endpointUrl += "/chat/completions";
                }

                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpointUrl);
                requestMessage.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                if (!string.IsNullOrWhiteSpace(_options.ApiKey))
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
                }

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

                var client = GetClient();
                var response = await client.SendAsync(requestMessage, cts.Token);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync(cts.Token);
                sw.Stop();

                using var doc = JsonDocument.Parse(responseJson);
                var content = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? string.Empty;

                var result = ParseAiContent(content);
                result.IsSuccess = true;
                result.ModelUsed = _options.Model;
                result.RawResponse = responseJson;
                result.MarkdownReport = content;
                result.ElapsedMs = sw.ElapsedMilliseconds;

                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogWarning(ex, "AI 根因智能分析调用失败: {Message}", ex.Message);

                return new AiAnalysisResultDto
                {
                    IsSuccess = false,
                    ModelUsed = _options.Model,
                    ElapsedMs = sw.ElapsedMilliseconds,
                    ErrorMessage = $"AI 推理异常: {ex.Message}"
                };
            }
        }

        private string BuildPrompt(FlightPackMetadata metadata, IReadOnlyList<FlightPackTimelineEvent> events, string baseNarrative)
        {
            var sb = new StringBuilder();
            sb.AppendLine("### 事故黑匣子全景参数");
            sb.AppendLine($"- 目标锚点: {metadata.Anchor.Type} = {metadata.Anchor.Key}");
            sb.AppendLine($"- 关联车辆: {metadata.Anchor.RelatedVehicleId ?? "无"}");
            sb.AppendLine($"- 时序范围: {metadata.TimeWindow.QueryStartTime:yyyy-MM-dd HH:mm:ss} ~ {metadata.TimeWindow.QueryEndTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            sb.AppendLine("### 基础因果叙事");
            sb.AppendLine(baseNarrative);
            sb.AppendLine();

            sb.AppendLine("### 关键时序事件流水 (前 40 项)");
            // 筛选异常、操作和关键 API 事件
            var keyEvents = events
                .Where(e => e.Level != "Information" || e.Track == "Operator" || (e.Track == "API" && e.Title.Contains("500")))
                .Take(40)
                .ToList();

            if (keyEvents.Count == 0)
            {
                keyEvents = events.Take(30).ToList();
            }

            foreach (var evt in keyEvents)
            {
                sb.AppendLine($"- [{evt.Timestamp:HH:mm:ss.fff}] [{evt.Track}] [{evt.Level}] {evt.Title} - {evt.Summary}");
            }

            return sb.ToString();
        }

        private AiAnalysisResultDto ParseAiContent(string content)
        {
            var dto = new AiAnalysisResultDto();

            // 提取【根因结论】
            var rootCauseMatch = Regex.Match(content, @"【根因结论】[：:]?\s*(.*?)(?=\n【|\n#|\n\n|$)", RegexOptions.Singleline);
            if (rootCauseMatch.Success)
            {
                dto.RootCauseSummary = rootCauseMatch.Groups[1].Value.Trim();
            }
            else
            {
                dto.RootCauseSummary = "大模型未按固定段落输出根因，详见推理报告全文";
            }

            // 提取【责任归属】
            var respMatch = Regex.Match(content, @"【责任归属】[：:]?\s*(.*?)(?=\n【|\n#|\n\n|$)", RegexOptions.Singleline);
            if (respMatch.Success)
            {
                dto.ResponsibleParty = respMatch.Groups[1].Value.Trim();
            }

            // 提取【置信度】
            var confMatch = Regex.Match(content, @"【置信度】[：:]?\s*(High|Medium|Low|高|中|低)", RegexOptions.IgnoreCase);
            if (confMatch.Success)
            {
                dto.ConfidenceLevel = confMatch.Groups[1].Value.Trim();
            }

            // 提取【推荐排障措施】
            var actionMatch = Regex.Match(content, @"【推荐排障措施】[：:]?\s*(.*?)(?=\n【|\n#|\n\n|$)", RegexOptions.Singleline);
            if (actionMatch.Success)
            {
                var actionText = actionMatch.Groups[1].Value;
                var lines = actionText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim().TrimStart('-', '*', '1', '2', '3', '4', '5', '.', ' '))
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToList();
                dto.RecommendedActions = lines;
            }

            return dto;
        }
    }
}

