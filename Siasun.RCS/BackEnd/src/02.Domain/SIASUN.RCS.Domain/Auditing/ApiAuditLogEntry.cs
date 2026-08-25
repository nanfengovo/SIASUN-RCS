

using System;

namespace SIASUN.RCS.Auditing
{
    /// <summary>
    /// API报文日志
    /// </summary>
    public class ApiAuditLogEntry
    {
        /// <summary>
        /// Id of the audit log entry.  
        /// </summary>
        public long Id { get; init; }

        /// <summary>
        /// TraceId
        /// </summary>

        public string TraceId { get; init; } = string.Empty;

        /// <summary>
        /// 方向（Inbound/Outbound）
        /// </summary>

        public Direction Direction { get; init; } = Direction.Inbound;

        /// <summary>
        /// 对接系统
        /// </summary>

        public string Peer { get; init; } = string.Empty;

        /// <summary>
        /// Http 方法 Post/Get/Put/Delete
        /// </summary>
        public HttpMethod HttpMethod { get; init; } = HttpMethod.Post;

        /// <summary>
        /// 请求路径
        /// </summary>

        public string Path { get; init; } = string.Empty;

        /// <summary>
        /// 响应码
        /// </summary>

        public int StatusCode { get; init; }


        /// <summary>
        /// 执行耗时单位是ms
        /// </summary>
        public long ElapsedMs { get; init; }

        /// <summary>
        /// 请求体
        /// </summary>

        public string? RequestBody { get; init; }

        /// <summary>
        /// 响应体
        /// </summary>

        public string? ResponseBody { get; init; }

        /// <summary>
        /// 客户端IP地址
        /// </summary>

        public string? ClientIpAddress { get; init; }

        /// <summary>
        /// 客户端名称
        /// </summary>

        public string? ClientName { get; init; }

        /// <summary>
        /// 异常信息
        /// </summary>

        public string? Exception { get; init; }

        /// <summary>
        /// 创建时间
        /// </summary>

        public DateTime CreationTime { get; init; } = DateTime.UtcNow;
    }
}