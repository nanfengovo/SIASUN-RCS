using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.IO;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Infrastructure.Logging.Filtering;

namespace SIASUN.RCS.Infrastructure.Logging
{
    public class InboundAuditMiddleware
    {
        /// <summary>
        /// 中间件执行下一步
        /// </summary>
        private readonly RequestDelegate _next;

        /// <summary>
        /// 报文流管理
        /// </summary>

        private readonly RecyclableMemoryStreamManager _streamManager;

        /// <summary>
        /// 缓存API日志写入，解耦用的
        /// </summary>

        private readonly ApiAuditLogChannel _channel;

        private readonly IAuditLogFilterEvaluator _filterEvaluator;

        public InboundAuditMiddleware(
            RequestDelegate next,
            RecyclableMemoryStreamManager streamManager,
            ApiAuditLogChannel channel,
            IAuditLogFilterEvaluator filterEvaluator)
        {
            _next = next;
            _streamManager = streamManager;
            _channel = channel;
            _filterEvaluator = filterEvaluator;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // 1. 基于内存高速规则引擎判定是否需要记录审计日志（白名单驱动 + 黑名单防御），通过后走中间件下一步
            if (!_filterEvaluator.ShouldAudit(path, context.Request.Method, Direction.Inbound))
            {
                await _next(context);
                return;
            }

            // 开始计时
            var sw = Stopwatch.StartNew();
            // 把流变成可缓存的
            context.Request.EnableBuffering();

            // 2.截取请求体
            string requestBody = string.Empty;
            if (context.Request.ContentLength > 0)
            {
                // 把底层报文（0，1）转为人类可读的UTF_8的 并且不关闭流，避免中间件下一步到控制器是空报文
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                // 读缓存的
                requestBody = await reader.ReadToEndAsync();
                // 回到报文开始地方
                context.Request.Body.Position = 0;
            }

            // 3.响应流拦截 存一份报文
            var originalBodyStream = context.Response.Body;
            // 获取一个容器放
            await using var memStream = _streamManager.GetStream();
            // 用memStream换context.Response.Body
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
                // 计时结束
                sw.Stop();
                // 回到报文开头
                memStream.Position = 0;

                using var respReader = new StreamReader(memStream, Encoding.UTF8, leaveOpen: true);
                responseBody = await respReader.ReadToEndAsync();
                memStream.Position = 0;

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
                    StatusCode = caughtException != null ? 500 : context.Response.StatusCode,
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