
namespace SIASUN.RCS.Monitor
{
    /// <summary>
    /// 系统资源全局监控视图模型
    /// </summary>
    public class SystemResourceMetricsDto
    {
        /// <summary>日志磁盘水位与容量状态</summary>
        public DiskMetricsDto Disk { get; set; } = new();

        /// <summary>当前进程内存占用状态</summary>
        public MemoryMetricsDto Memory { get; set; } = new();
    }

    /// <summary>
    /// 磁盘状态与水位线指标
    /// </summary>
    public class DiskMetricsDto
    {
        /// <summary>是否已开启基于高水位的强制自动清理保护功能</summary>
        public bool IsSelfHealEnabled { get; set; }

        /// <summary>高水位警戒线百分比 (触及此红线即强制清理，例: 85)</summary>
        public int HighWatermark { get; set; }

        /// <summary>低水位安全线百分比 (自愈清理直到降至此绿线，例: 70)</summary>
        public int LowWatermark { get; set; }

        /// <summary>磁盘物理总容量 (Byte)</summary>
        public long TotalSizeBytes { get; set; }

        /// <summary>磁盘当前已用容量 (Byte)</summary>
        public long UsedSizeBytes { get; set; }

        /// <summary>磁盘当前可用容量 (Byte)</summary>
        public long FreeSizeBytes { get; set; }

        /// <summary>综合占用百分比 (用于直接在前端仪表盘上渲染进度条 0-100)</summary>
        public int UsedPercentage { get; set; }

        /// <summary>挂载点或驱动器名称 (例: "C:\" 或 "/")</summary>
        public string DriveName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 内存占用状态指标
    /// </summary>
    public class MemoryMetricsDto
    {
        /// <summary>当前后台服务进程占用的物理工作集大小 (Byte)</summary>
        public long WorkingSet64 { get; set; }
    }
}

