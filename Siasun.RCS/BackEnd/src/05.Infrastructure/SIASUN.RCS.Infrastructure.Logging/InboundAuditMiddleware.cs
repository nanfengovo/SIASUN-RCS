using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IO;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Infrastructure.Logging.Filtering;
using SIASUN.RCS.Infrastructure.Logging.Masking;
using Volo.Abp.Tracing;

namespace SIASUN.RCS.Infrastructure.Logging
{
    /// <summary>
    /// 入站 HTTP 报文审计拦截中间件
    /// 负责截取外部上游接口调用的原始报文、统一 TraceId 贯穿、计算耗时并异步推入落盘通道
    /// </summary>
    public class InboundAuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RecyclableMemoryStreamManager _streamManager;
        private readonly ApiAuditLogChannel _channel;
        private readonly IAuditLogFilterEvaluator _filterEvaluator;
        private readonly Diagnostics.SignalR.IDiagnosticLiveStreamBroker? _liveStreamBroker;
        private readonly ICorrelationIdProvider? _correlationIdProvider;

        /// <summary>
        /// 构造函数注入中间件所需组件与链路追踪提供者
        /// </summary>
        public InboundAuditMiddleware(
            RequestDelegate next,
            RecyclableMemoryStreamManager streamManager,
            ApiAuditLogChannel channel,
            IAuditLogFilterEvaluator filterEvaluator,
            Diagnostics.SignalR.IDiagnosticLiveStreamBroker? liveStreamBroker = null,
            ICorrelationIdProvider? correlationIdProvider = null)
        {
            _next = next;
            _streamManager = streamManager;
            _channel = channel;
            _filterEvaluator = filterEvaluator;
            _liveStreamBroker = liveStreamBroker;
            _correlationIdProvider = correlationIdProvider;
        }

        /// <summary>
        /// 中间件请求拦截处理主逻辑
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // 1. 基于内存高速规则引擎判定是否需要记录审计日志（白名单驱动 + 黑名单防御），通过后走中间件下一步
            if (!_filterEvaluator.ShouldAudit(path, context.Request.Method, Direction.Inbound))
            {
                await _next(context);
                return;
            }

            // 解析端点特性中的对接系统名称 (Peer)
            var endpoint = context.GetEndpoint();
            var peerName = endpoint?.Metadata.GetMetadata<AuditPeerAttribute>()?.PeerName ?? "Unknown";

            // 2. 统一解析与维护全局 TraceId / CorrelationId 锚点
            var traceId = ResolveTraceId(context);
            using var traceScope = SIASUN.RCS.Diagnostics.RcsTraceContext.SetScoped(traceId);

            // 锁死写入请求头与上下文 Items，保证后续 EF 实体拦截器与业务操作审计获得完全一致的 TraceId
            if (!context.Request.Headers.ContainsKey("X-Correlation-Id"))
            {
                context.Request.Headers["X-Correlation-Id"] = traceId;
            }
            context.Items["__RcsCorrelationId"] = traceId;

            // 确保回写至响应头，支撑外部系统联调与事故反查
            if (!context.Response.Headers.ContainsKey("X-Correlation-Id"))
            {
                context.Response.Headers["X-Correlation-Id"] = traceId;
            }

            // 开始计时
            var sw = Stopwatch.StartNew();
            // 把流变成可缓存的
            context.Request.EnableBuffering();

            // 3. 截取请求体
            string requestBody = string.Empty;
            if (context.Request.ContentLength > 0)
            {
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                requestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }

            // 4. 响应流拦截
            var originalBodyStream = context.Response.Body;
            await using var memStream = _streamManager.GetStream();
            context.Response.Body = memStream;

            string? responseBody = null;
            Exception? caughtException = null;

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                caughtException = ex;
                throw;
            }
            finally
            {
                sw.Stop();
                memStream.Position = 0;

                using var respReader = new StreamReader(memStream, Encoding.UTF8, leaveOpen: true);
                responseBody = await respReader.ReadToEndAsync();
                memStream.Position = 0;

                await memStream.CopyToAsync(originalBodyStream);
                context.Response.Body = originalBodyStream;

                // 解析 HttpMethod 枚举
                _ = Enum.TryParse<HttpMethod>(context.Request.Method, true, out var methodEnum);

                var maskedRequestBody = AuditDataMasker.Mask(TruncateBody(requestBody));
                var maskedResponseBody = AuditDataMasker.Mask(TruncateBody(responseBody));

                // 5. 组装实体并无阻塞推入 Channel
                _channel.TryWrite(new ApiAuditLogEntry
                {
                    TraceId = traceId,
                    Direction = Direction.Inbound,
                    Peer = peerName,
                    HttpMethod = methodEnum,
                    Path = path,
                    StatusCode = caughtException != null ? 500 : context.Response.StatusCode,
                    ElapsedMs = sw.ElapsedMilliseconds,
                    RequestBody = maskedRequestBody,
                    ResponseBody = maskedResponseBody,
                    ClientIpAddress = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                    ClientName = context.User.Identity?.Name,
                    Exception = caughtException?.Message
                });

                if (_liveStreamBroker != null && _liveStreamBroker.IsEnabled)
                {
                    var isErr = context.Response.StatusCode >= 500 || caughtException != null;
                    var isWarn = context.Response.StatusCode >= 400 && context.Response.StatusCode < 500;
                    _liveStreamBroker.Publish(new Diagnostics.SignalR.LiveEventDto
                    {
                        Timestamp = DateTime.UtcNow,
                        Track = "API",
                        Level = isErr ? "Error" : (isWarn ? "Warning" : "Information"),
                        Source = string.IsNullOrEmpty(peerName) ? "API" : peerName,
                        Title = $"{context.Request.Method} {context.Request.Path} ({context.Response.StatusCode})",
                        Summary = $"耗时: {sw.ElapsedMilliseconds}ms, 客户端: {context.Connection.RemoteIpAddress}",
                        TraceId = traceId
                    });
                }
            }
        }

        private string ResolveTraceId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var incomingCid) && !string.IsNullOrWhiteSpace(incomingCid))
            {
                return incomingCid.ToString();
            }

            if (context.Request.Headers.TryGetValue("X-Trace-Id", out var incomingTid) && !string.IsNullOrWhiteSpace(incomingTid))
            {
                return incomingTid.ToString();
            }

            var provider = _correlationIdProvider ?? context.RequestServices?.GetService<ICorrelationIdProvider>();
            var providerCid = provider?.Get();
            var ambient = SIASUN.RCS.Diagnostics.RcsTraceContext.CurrentTraceId;
            if (!string.IsNullOrWhiteSpace(ambient))
            {
                return ambient;
            }

            return !string.IsNullOrWhiteSpace(context.TraceIdentifier) ? context.TraceIdentifier : Guid.NewGuid().ToString("N");
        }

        private static string? TruncateBody(string? body, int maxLen = 65536)
        {
            if (string.IsNullOrEmpty(body)) return body;
            return body.Length <= maxLen ? body : body[..maxLen] + " [TRUNCATED]";
        }
    }
}