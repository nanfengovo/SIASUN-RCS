using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Infrastructure.Logging.Filtering;
using HttpMethod = SIASUN.RCS.Auditing.HttpMethod;

namespace SIASUN.RCS.Infrastructure.Logging
{
    public class OutboundAuditDelegatingHandler : DelegatingHandler
    {
        private readonly ApiAuditLogChannel _channel;

        private readonly IAuditLogFilterEvaluator _filterEvaluator;

        public OutboundAuditDelegatingHandler(ApiAuditLogChannel channel, IAuditLogFilterEvaluator filterEvaluator)
        {
            _channel = channel;
            _filterEvaluator = filterEvaluator;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            // 判断是否需要拦截（调用黑名单派出）
            if (!_filterEvaluator.ShouldAudit(path, request.Method.Method, Auditing.Direction.Outbound))
            {
                return await base.SendAsync(request, cancellationToken);
            }

            var sw = Stopwatch.StartNew();
            string requestBody = string.Empty;
            // 2. 安全读取请求体（重点防坑！）
            if (request.Content != null)
            {
                //先加载到缓冲，否则base.SendAsync 请求时会bao流已经消耗
                await request.Content.LoadIntoBufferAsync();
                requestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            string? responseBody = null;
            Exception? caughtException = null;
            HttpResponseMessage? response = null;

            try
            {
                // 3.真正执行的网络请求调用
                response = await base.SendAsync(request, cancellationToken);

                // 4. 安全读取响应体
                if (response.Content != null)
                {
                    await response.Content.LoadIntoBufferAsync();
                    responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                // 捕获网络层面的异常（连接被拒，对方宕机）
                caughtException = ex;
                throw;
            }
            finally
            {
                sw.Stop();
                Enum.TryParse<HttpMethod>(request.Method.Method, true, out var methodEnum);

                // 5. 组装实体并推入内存无锁通道（与Inbound 共享落库链路）
                _channel.TryWrite(new ApiAuditLogEntry
                {
                    TraceId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N"),
                    Direction = Direction.Outbound,
                    Peer = ResolvePeer(request.RequestUri),
                    HttpMethod = methodEnum,
                    Path = path,
                    StatusCode = caughtException != null ? 500 : (int)(response?.StatusCode ?? System.Net.HttpStatusCode.InternalServerError),
                    ElapsedMs = sw.ElapsedMilliseconds,
                    RequestBody = TruncateBody(requestBody),
                    ResponseBody = TruncateBody(responseBody),
                    ClientIpAddress = "localhost",
                    Exception = caughtException?.Message
                });
            }
            return response!;
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