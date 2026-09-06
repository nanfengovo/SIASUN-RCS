using System;

namespace SIASUN.RCS.Diagnostics
{
    /// <summary>
    /// 工控机磁盘空间达到高水位触发自愈清理领域的事件
    /// 遵循《AGENTS.md》铁律 7：跨领域副作用通过领域事件解耦，通知与实时告警统一由独立 EventHandler 处理
    /// </summary>
    public class DiskSelfHealingTriggeredEvent
    {
        /// <summary>
        /// 清理前磁盘使用率百分比 (0-100)
        /// </summary>
        public int PreUsagePercent { get; set; }

        /// <summary>
        /// 触发的高水位阈值百分比 (0-100)
        /// </summary>
        public int HighWatermark { get; set; }

        /// <summary>
        /// 目标清理的低水位阈值百分比 (0-100)
        /// </summary>
        public int LowWatermark { get; set; }

        /// <summary>
        /// 清理后磁盘使用率百分比 (0-100，自愈完成后回填)
        /// </summary>
        public int PostUsagePercent { get; set; }

        /// <summary>
        /// 累计释放的磁盘存储空间 (字节)
        /// </summary>
        public long ReleasedBytes { get; set; }

        /// <summary>
        /// 本次清理强制删除的旧日志分片文件数
        /// </summary>
        public int DeletedFileCount { get; set; }

        /// <summary>
        /// 自愈事件警报或详情消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 触发自愈的时间戳 (UTC)
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 无参构造函数 (支持序列化与反序列化)
        /// </summary>
        public DiskSelfHealingTriggeredEvent()
        {
        }

        /// <summary>
        /// 初始化磁盘高水位自愈触发事件
        /// </summary>
        /// <param name="preUsagePercent">清理前使用率</param>
        /// <param name="highWatermark">高水位阈值</param>
        /// <param name="lowWatermark">低水位阈值</param>
        /// <param name="message">警告消息</param>
        public DiskSelfHealingTriggeredEvent(
            int preUsagePercent,
            int highWatermark,
            int lowWatermark,
            string message)
        {
            PreUsagePercent = preUsagePercent;
            HighWatermark = highWatermark;
            LowWatermark = lowWatermark;
            Message = message;
            Timestamp = DateTime.UtcNow;
        }
    }
}

