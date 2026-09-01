using Volo.Abp.Settings;

namespace SIASUN.RCS.Monitor
{
    public class RCSMonitorSettingDefinitionProvider : SettingDefinitionProvider
    {
        public override void Define(ISettingDefinitionContext context)
        {
            context.Add(
                new SettingDefinition(
                    RCSMonitorSettings.IsDiskSelfHealEnabled,
                    "true",
                    displayName: new Volo.Abp.Localization.FixedLocalizableString("是否启用磁盘高水位自愈防护"),
                    description: new Volo.Abp.Localization.FixedLocalizableString("启用后，当日志所在磁盘空间达到高水位时，会自动强制清理最旧的审计日志"),
                    isVisibleToClients: true
                ),
                new SettingDefinition(
                    RCSMonitorSettings.DiskHighWatermark,
                    "85",
                    displayName: new Volo.Abp.Localization.FixedLocalizableString("磁盘高水位阈值(%)"),
                    description: new Volo.Abp.Localization.FixedLocalizableString("磁盘已用空间百分比达到此值时，触发紧急清理"),
                    isVisibleToClients: true
                ),
                new SettingDefinition(
                    RCSMonitorSettings.DiskLowWatermark,
                    "70",
                    displayName: new Volo.Abp.Localization.FixedLocalizableString("磁盘低水位阈值(%)"),
                    description: new Volo.Abp.Localization.FixedLocalizableString("紧急清理动作将持续删除旧文件，直到磁盘已用空间降至此水位停止"),
                    isVisibleToClients: true
                ),
                new SettingDefinition(
                    RCSMonitorSettings.HardRetentionHours,
                    "0",
                    displayName: new Volo.Abp.Localization.FixedLocalizableString("强制保留时长(小时)"),
                    description: new Volo.Abp.Localization.FixedLocalizableString("在紧急自愈时，哪怕尚未降至低水位，也绝对不允许删除生成时间在此时间内的文件 (0表示无底线清理)"),
                    isVisibleToClients: true
                )
            );
        }
    }
}
