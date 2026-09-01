using System;

namespace SIASUN.RCS.Auditing
{
    /// <summary>
    /// 用于标记当前 Controller 或 Action 所属的对接系统 (Peer)。
    /// 中间件会通过解析此特性自动判断报文日志中的 Peer 字段。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AuditPeerAttribute : Attribute
    {
        public string PeerName { get; }

        public AuditPeerAttribute(string peerName)
        {
            if (string.IsNullOrWhiteSpace(peerName))
            {
                throw new ArgumentException("PeerName cannot be null or empty.", nameof(peerName));
            }
            PeerName = peerName;
        }
    }
}

