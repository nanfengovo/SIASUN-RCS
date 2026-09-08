using System;
using System.Net.Http;

namespace SIASUN.RCS.Infrastructure.Resilience
{
    /// <summary>
    /// 工业级高吞吐长连接 SocketsHttpHandler 工厂
    /// 避免经典 HttpClient 套接字耗尽（Socket Exhaustion）与 DNS 停滞问题，针对工控机现场网络优化
    /// </summary>
    public static class ResilientSocketsHttpHandlerFactory
    {
        /// <summary>
        /// 创建并配置优化的 SocketsHttpHandler 实例
        /// </summary>
        public static SocketsHttpHandler CreateHandler()
        {
            return new SocketsHttpHandler
            {
                // 连接池生命周期，避免 DNS 漂移失效
                PooledConnectionLifetime = TimeSpan.FromMinutes(15),

                // 连接空闲保留时间
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),

                // 单个端点最大并发连接数（工控机上游通常为单机/内网服务，限流保护）
                MaxConnectionsPerServer = 50,

                // 快速建立连接超时
                ConnectTimeout = TimeSpan.FromSeconds(5),

                // 保持心跳活动以穿透现场交换机防火墙 NAT 保持期
                KeepAlivePingDelay = TimeSpan.FromSeconds(30),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(5),
                KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,

                // 启用 HTTP/2 多路复用
                EnableMultipleHttp2Connections = true
            };
        }
    }
}
