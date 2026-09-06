using System;
using System.Collections.Generic;

namespace SIASUN.RCS.Monitor
{
    /// <summary>
    /// 系统容量健康评级
    /// </summary>
    public enum CapacityHealthLevel
    {
        /// <summary>
        /// 健康稳定（容量与日志行数处于绿色安全水位）
        /// </summary>
        Healthy = 0,

        /// <summary>
        /// 预警（触及预警阈值，提示运维人员关注或规划扩容）
        /// </summary>
        Warning = 1,

        /// <summary>
        /// 严重告警（触及高水位红线，必须立即触发自愈自清理或人工介入）
        /// </summary>
        Critical = 2
    }

    /// <summary>
    /// 长期容量可观测与前瞻告警健康报告（L4 自治观测模型）
    /// </summary>
    public class CapacityHealthReportDto
    {
        /// <summary>
        /// 综合容量健康等级
        /// </summary>
        public CapacityHealthLevel OverallHealth { get; set; } = CapacityHealthLevel.Healthy;

        /// <summary>
        /// 磁盘使用百分比 (0-100)
        /// </summary>
        public int DiskUsagePercentage { get; set; }

        /// <summary>
        /// 磁盘健康等级
        /// </summary>
        public CapacityHealthLevel DiskHealth { get; set; } = CapacityHealthLevel.Healthy;

        /// <summary>
        /// 日志目录总物理占用大小 (Byte)
        /// </summary>
        public long LogDirectorySizeBytes { get; set; }

        /// <summary>
        /// 数据库持久化日志总行数（OperationLogs + SystemEventLogs）
        /// </summary>
        public long DatabaseLogTotalRows { get; set; }

        /// <summary>
        /// 数据库日志膨胀健康等级
        /// </summary>
        public CapacityHealthLevel DatabaseLogHealth { get; set; } = CapacityHealthLevel.Healthy;

        /// <summary>
        /// 当前触发的主动容量告警条目清单
        /// </summary>
        public List<string> ActiveAlerts { get; set; } = new();

        /// <summary>
        /// 容量评估生成时间 (UTC)
        /// </summary>
        public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
    }
}
