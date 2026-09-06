using System;
using System.Collections.Generic;

namespace SIASUN.RCS.Monitor
{
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
        /// API 审计日志通道当前待消费积压深度
        /// </summary>
        public int ApiChannelDepth { get; set; }

        /// <summary>
        /// 实体变更审计日志通道当前待消费积压深度
        /// </summary>
        public int EntityChannelDepth { get; set; }

        /// <summary>
        /// SignalR 实时诊断推流中台当前待推送事件积压数
        /// </summary>
        public int LiveStreamPendingCount { get; set; }

        /// <summary>
        /// 调度员操作审计通道当前待消费积压深度
        /// </summary>
        public int OperationChannelDepth { get; set; }

        /// <summary>
        /// 当前待消费处理的特权审计证据紧急溢流条目数（若大于 0 说明正处于应急保全未完全消化状态）
        /// </summary>
        public int PendingSpillCount { get; set; }

        /// <summary>
        /// 特权审计证据应急溢流落盘累计计数（一旦大于 0 说明瞬时满负荷触发了本地文件落盘保全）
        /// </summary>
        public long PrivilegeSpillCount { get; set; }

        /// <summary>
        /// 累计应急落盘本地磁盘写入失败次数（若大于 0 说明工控机本地存储写保护、空间耗尽或 I/O 故障，严重威胁铁证零丢失）
        /// </summary>
        public long SpillDiskWriteFailures { get; set; }

        /// <summary>
        /// 特权审计证据保全健康等级（若有特权证据处于未消化溢流或磁盘写入失败则标为 Critical；历史已全部消化则降为 Warning 恢复系统自愈）
        /// </summary>
        public CapacityHealthLevel PrivilegeSpillHealth { get; set; } = CapacityHealthLevel.Healthy;

        /// <summary>
        /// 自适应限流调控器当前瞬时速率 (EPS)
        /// </summary>
        public double GovernorCurrentEps { get; set; }

        /// <summary>
        /// 自适应限流调控器累计降采样丢弃常规/遥测事件数
        /// </summary>
        public long GovernorDropCount { get; set; }

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

