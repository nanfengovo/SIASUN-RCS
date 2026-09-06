using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Infrastructure.Logging.Filtering;
using SIASUN.RCS.Infrastructure.Logging.Masking;
using Volo.Abp.Tracing;
using HttpMethod = SIASUN.RCS.Auditing.HttpMethod;

namespace SIASUN.RCS.Infrastructure.Logging
{
    /// <summary>
    /// 出站 HTTP 报文审计委托处理程序
    /// 负责拦截向底层 TM 车体控制、立库、MES 等外部服务发起的调用，贯穿 TraceId 并记录原始报文
    /// </summary>
    public class OutboundAuditDelegatingHandler : DelegatingHandler
    {
        private readonly ApiAuditLogChannel _channel;
        private readonly IAuditLogFilterEvaluator _filterEvaluator;
        private readonly ICorrelationIdProvider? _correlationIdProvider;

        /// <summary>
        /// 构造函数注入审计通道、过滤器与链路追踪提供者
        /// </summary>
        public OutboundAuditDelegatingHandler(
            ApiAuditLogChannel channel,
            IAuditLogFilterEvaluator filterEvaluator,
            ICorrelationIdProvider? correlationIdProvider = null)
        {
            _channel = channel;
            _filterEvaluator = filterEvaluator;
            _correlationIdProvider = correlationIdProvider;
        }

        /// <summary>
        /// 发送外部请求并拦截审计出站报文
        /// </summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            // 1. 判断是否需要拦截（黑白名单规则判定）
            if (!_filterEvaluator.ShouldAudit(path, request.Method.Method, Direction.Outbound))
            {
                return await base.SendAsync(request, cancellationToken);
            }

            // 2. 贯穿 TraceId / CorrelationId 到出站请求头
            var traceId = ResolveTraceId(request);
            if (!request.Headers.Contains("X-Correlation-Id"))
            {
                request.Headers.TryAddWithoutValidation("X-Correlation-Id", traceId);
            }

            var sw = Stopwatch.StartNew();
            string requestBody = string.Empty;

            // 3. 安全读取请求体
            if (request.Content != null)
            {
                await request.Content.LoadIntoBufferAsync();
                requestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            string? responseBody = null;
            Exception? caughtException = null;
            HttpResponseMessage? response = null;

            try
            {
                // 4. 真正执行外部网络调用
                response = await base.SendAsync(request, cancellationToken);

                // 5. 安全读取响应体
                if (response.Content != null)
                {
                    await response.Content.LoadIntoBufferAsync();
                    responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                caughtException = ex;
                throw;
            }
            finally
            {
                sw.Stop();
                _ = Enum.TryParse<HttpMethod>(request.Method.Method, true, out var methodEnum);

                var maskedRequestBody = AuditDataMasker.Mask(TruncateBody(requestBody));
                var maskedResponseBody = AuditDataMasker.Mask(TruncateBody(responseBody));

                // 6. 组装实体并推入内存无锁通道
                _channel.TryWrite(new ApiAuditLogEntry
                {
                    TraceId = traceId,
                    Direction = Direction.Outbound,
                    Peer = ResolvePeer(request.RequestUri),
                    HttpMethod = methodEnum,
                    Path = path,
                    StatusCode = caughtException != null ? 500 : (int)(response?.StatusCode ?? System.Net.HttpStatusCode.InternalServerError),
                    ElapsedMs = sw.ElapsedMilliseconds,
                    RequestBody = maskedRequestBody,
                    ResponseBody = maskedResponseBody,
                    ClientIpAddress = "localhost",
                    Exception = caughtException?.Message
                });
            }

            return response!;
        }

        private string ResolveTraceId(HttpRequestMessage request)
        {
            if (request.Headers.TryGetValues("X-Correlation-Id", out var values))
            {
                var val = values.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(val)) return val;
            }

            var cid = _correlationIdProvider?.Get();
            if (!string.IsNullOrWhiteSpace(cid))
            {
                return cid;
            }

            var ambient = SIASUN.RCS.Diagnostics.RcsTraceContext.CurrentTraceId;
            if (!string.IsNullOrWhiteSpace(ambient))
            {
                return ambient;
            }

            return Guid.NewGuid().ToString("N");
        }

        private static string? TruncateBody(string? body, int maxLen = 65536)
        {
            if (string.IsNullOrEmpty(body)) return body;
            return body.Length <= maxLen ? body : body[..maxLen] + " [TRUNCATED]";
        }

        private static string ResolvePeer(Uri? uri)
        {
            if (uri == null) return "Unknown";
            var host = uri.Host.ToLowerInvariant();
            var path = uri.AbsolutePath.ToLowerInvariant();

            if (host.Contains("tm") || path.Contains("/tm/")) return "TM";
            if (host.Contains("mes") || path.Contains("/mes/")) return "MES";

            return "Unknown";
        }
    }
}