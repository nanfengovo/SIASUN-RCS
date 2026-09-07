using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace SIASUN.RCS.Locations
{
    /// <summary>
    /// 库位与物理设备 PLC 硬件联锁配置实体（可选独立扩展表）
    /// 遵循六边形架构与微内核插件化哲学，无 PLC 工位零侵入，机台口按需配置
    /// </summary>
    public class LocationPlcConfig : AuditedEntity<Guid>
    {
        /// <summary>
        /// 关联的库位或工位编码（唯一索引）
        /// </summary>
        public string LocationCode { get; private set; } = string.Empty;

        /// <summary>
        /// 是否启用 PLC 物理硬件联锁检测
        /// </summary>
        public bool IsEnabled { get; private set; }

        /// <summary>
        /// 绑定的 PLC 网关适配器标识（如 "PLC_MOLDING_01"）
        /// </summary>
        public string? GatewayId { get; private set; }

        /// <summary>
        /// 料位状态检测 Tag（如 "DB100.DBX0.0"，取放料前核验物料在位状态）
        /// </summary>
        public string? MaterialPresenceTag { get; private set; }

        /// <summary>
        /// 对接就绪联锁 Tag（如 "DB100.DBX0.1"，机台允许小车入叉安全信号）
        /// </summary>
        public string? InterlockReadyTag { get; private set; }

        /// <summary>
        /// 联锁等待超时时间（秒，默认 30s，遵循 Fail-Closed 安全铁律，超时严禁伸叉）
        /// </summary>
        public int TimeoutSeconds { get; private set; }

        /// <summary>
        /// 配置描述或现场机台工艺备注
        /// </summary>
        public string? Description { get; private set; }

        /// <summary>
        /// EF Core 所需受保护构造函数
        /// </summary>
        protected LocationPlcConfig()
        {
        }

        /// <summary>
        /// 创建库位 PLC 硬件联锁配置
        /// </summary>
        /// <param name="id">配置主键</param>
        /// <param name="locationCode">库位编码</param>
        /// <param name="isEnabled">是否启用联锁</param>
        /// <param name="gatewayId">PLC 网关标识</param>
        /// <param name="materialPresenceTag">在位料位 Tag</param>
        /// <param name="interlockReadyTag">对接就绪 Tag</param>
        /// <param name="timeoutSeconds">超时阈值（秒）</param>
        /// <param name="description">配置备注</param>
        public LocationPlcConfig(
            Guid id,
            string locationCode,
            bool isEnabled,
            string? gatewayId = null,
            string? materialPresenceTag = null,
            string? interlockReadyTag = null,
            int timeoutSeconds = 30,
            string? description = null) : base(id)
        {
            LocationCode = Check.NotNullOrWhiteSpace(locationCode, nameof(locationCode), maxLength: LocationLockConsts.MaxLocationCodeLength);
            IsEnabled = isEnabled;
            GatewayId = gatewayId;
            MaterialPresenceTag = materialPresenceTag;
            InterlockReadyTag = interlockReadyTag;
            TimeoutSeconds = timeoutSeconds > 0 ? timeoutSeconds : 30;
            Description = description;
        }

        /// <summary>
        /// 更新 PLC 联锁配置参数
        /// </summary>
        public void Update(
            bool isEnabled,
            string? gatewayId,
            string? materialPresenceTag,
            string? interlockReadyTag,
            int timeoutSeconds,
            string? description)
        {
            IsEnabled = isEnabled;
            GatewayId = gatewayId;
            MaterialPresenceTag = materialPresenceTag;
            InterlockReadyTag = interlockReadyTag;
            TimeoutSeconds = timeoutSeconds > 0 ? timeoutSeconds : 30;
            Description = description;
        }
    }
}
