namespace SIASUN.RCS.Monitor
{
    public static class RCSMonitorSettings
    {
        private const string Prefix = "RCS.Monitor.SelfHeal";

        public const string IsDiskSelfHealEnabled = Prefix + ".IsDiskSelfHealEnabled";
        public const string DiskHighWatermark = Prefix + ".DiskHighWatermark";
        public const string DiskLowWatermark = Prefix + ".DiskLowWatermark";
        public const string HardRetentionHours = Prefix + ".HardRetentionHours";
    }
}

