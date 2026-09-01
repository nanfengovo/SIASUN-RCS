using System.Collections.Generic;

namespace SIASUN.RCS.Monitor
{
    public class SystemResourceMetricsDto
    {
        public DiskMetricsDto Disk { get; set; } = new();
        public MemoryMetricsDto Memory { get; set; } = new();
        // CPU metrics can be added later as needed
    }

    public class DiskMetricsDto
    {
        public bool IsSelfHealEnabled { get; set; }
        public int HighWatermark { get; set; }
        public int LowWatermark { get; set; }
        public long TotalSizeBytes { get; set; }
        public long UsedSizeBytes { get; set; }
        public long FreeSizeBytes { get; set; }
        public int UsedPercentage { get; set; }
        public string DriveName { get; set; } = string.Empty;
    }

    public class MemoryMetricsDto
    {
        public long WorkingSet64 { get; set; }
    }
}

