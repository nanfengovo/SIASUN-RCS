using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.IO;
using SIASUN.RCS.Auditing;

namespace SIASUN.RCS.Infrastructure.Logging
{
    public class InboundAuditMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly RecyclableMemoryStreamManager _streamManager;

        private readonly ApiAuditLogChannel _channel;

        public InboundAuditMiddleware(RequestDelegate next, RecyclableMemoryStreamManager streamManager, ApiAuditLogChannel channel)
        {
            _next = next;
            _streamManager = streamManager;
            _channel = channel;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // 1.过滤高频探测与静态资源
            if (path.StartsWith("/health") || path.StartsWith("/hubs/") || path.EndsWith(".js") || path.EndsWith(".css") || path.EndsWith(".ico"))
            {
                await _next(context);
                return;
            }

            var sw = Stopwatch.StartNew();
            context.Request.EnableBuffering();

            // 2.截取请求体
            string requestBody = string.Empty;
            if (context.Request.ContentLength > 0)
            {
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                requestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }

            // 3.响应流拦截
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
                using (var respReader = new StreamReader(memStream, Encoding.UTF8, leaveOpen: true))
                {
                    responseBody = await respReader.ReadToEndAsync();
                    memStream.Position = 0;
                }

                await memStream.CopyToAsync(originalBodyStream);
                context.Response.Body = originalBodyStream;

                // 4.解析HttpMethod枚举
                bool v = Enum.TryParse<HttpMethod>(context.Request.Method, true, out var methodEnum);

                // 5. 组装实体并无阻塞推入Channel
                _channel.TryWrite(new ApiAuditLogEntry
                {
                    TraceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier,
                    Direction = Direction.Inbound,
                    Peer = ResolvePeer(path),
                    HttpMethod = methodEnum,
                    Path = path,
                    StatusCode = 500,//避免在异常的时候还是200 这样更规范
                    ElapsedMs = sw.ElapsedMilliseconds,
                    RequestBody = TruncateBody(requestBody),
                    ResponseBody = TruncateBody(responseBody),
                    ClientIpAddress = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                    ClientName = context.User.Identity?.Name,
                    Exception = caughtException?.Message
                });
            }

        }

        private static string? TruncateBody(string? body, int maxLen = 65536)
        {
            if (string.IsNullOrEmpty(body)) return body;
            return body.Length <= maxLen ? body : body[..maxLen] + " [TRUNCATED]";
        }
        private static string ResolvePeer(string path)
        {
            if (path.Contains("/xinsong/", StringComparison.OrdinalIgnoreCase) || path.Contains("/tm/", StringComparison.OrdinalIgnoreCase)) return "TM";
            if (path.Contains("/mes/", StringComparison.OrdinalIgnoreCase)) return "MES";
            return "Unknown";
        }
    }
}