using System;

namespace SIASUN.RCS.Diagnostics.FlightPack
{
    /// <summary>
    /// 事故排障黑匣子收集打包请求领域对象
    /// </summary>
    public class FlightPackRequest
    {
        /// <summary>
        /// 锚点类型 (Task / Vehicle / TimeRange)
        /// </summary>
        public string AnchorType { get; set; } = "Task";

        /// <summary>
        /// 锚点标识 (任务号、车体号等)
        /// </summary>
        public string AnchorKey { get; set; } = string.Empty;

        /// <summary>
        /// 时序检索起始时间
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 时序检索结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 事故前缓冲分钟数
        /// </summary>
        public int BufferBeforeMinutes { get; set; } = 5;

        /// <summary>
        /// 事故后缓冲分钟数
        /// </summary>
        public int BufferAfterMinutes { get; set; } = 5;

        /// <summary>
        /// 导出操作人用户标识
        /// </summary>
        public Guid? ExportedByUserId { get; set; }

        /// <summary>
        /// 导出操作人姓名
        /// </summary>
        public string ExportedByUserName { get; set; } = "System";

        /// <summary>
        /// 导出客户端 IP
        /// </summary>
        public string ClientIp { get; set; } = string.Empty;

        /// <summary>
        /// 是否启用 AI 深度根因推理诊断
        /// </summary>
        public bool EnableAiAnalysis { get; set; } = false;
    }
}
