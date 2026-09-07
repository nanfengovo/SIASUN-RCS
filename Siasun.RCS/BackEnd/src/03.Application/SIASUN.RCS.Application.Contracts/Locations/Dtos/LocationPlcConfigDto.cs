using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace SIASUN.RCS.Locations.Dtos
{
    /// <summary>
    /// 库位 PLC 硬件联锁配置数据传输对象
    /// </summary>
    public class LocationPlcConfigDto : AuditedEntityDto<Guid>
    {
        /// <summary>
        /// 库位编码
        /// </summary>
        public string LocationCode { get; set; } = string.Empty;

        /// <summary>
        /// 是否启用 PLC 物理硬件联锁检测
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 绑定的 PLC 网关适配器标识（如 "PLC_MOLDING_01"）
        /// </summary>
        public string? GatewayId { get; set; }

        /// <summary>
        /// 料位状态检测 Tag（如 "DB100.DBX0.0"，取放料前核验物料在位状态）
        /// </summary>
        public string? MaterialPresenceTag { get; set; }

        /// <summary>
        /// 对接就绪联锁 Tag（如 "DB100.DBX0.1"，机台允许小车入叉安全信号）
        /// </summary>
        public string? InterlockReadyTag { get; set; }

        /// <summary>
        /// 联锁等待超时时间（秒，默认 30s，遵循 Fail-Closed 安全铁律，超时严禁伸叉）
        /// </summary>
        public int TimeoutSeconds { get; set; }

        /// <summary>
        /// 配置描述或现场机台工艺备注
        /// </summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// 创建库位 PLC 联锁配置输入 DTO
    /// </summary>
    public class CreateLocationPlcConfigDto
    {
        /// <summary>
        /// 关联的库位或工位编码
        /// </summary>
        [Required]
        [StringLength(LocationLockConsts.MaxLocationCodeLength)]
        public string LocationCode { get; set; } = string.Empty;

        /// <summary>
        /// 是否启用硬件联锁
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// PLC 网关驱动标识
        /// </summary>
        [StringLength(LocationLockConsts.MaxGatewayIdLength)]
        public string? GatewayId { get; set; }

        /// <summary>
        /// 料位检测 Tag
        /// </summary>
        [StringLength(LocationLockConsts.MaxTagLength)]
        public string? MaterialPresenceTag { get; set; }

        /// <summary>
        /// 允许入叉就绪 Tag
        /// </summary>
        [StringLength(LocationLockConsts.MaxTagLength)]
        public string? InterlockReadyTag { get; set; }

        /// <summary>
        /// 超时等待阈值（秒）
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// 备注描述
        /// </summary>
        [StringLength(512)]
        public string? Description { get; set; }
    }

    /// <summary>
    /// 更新库位 PLC 联锁配置输入 DTO
    /// </summary>
    public class UpdateLocationPlcConfigDto
    {
        /// <summary>
        /// 是否启用硬件联锁
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// PLC 网关驱动标识
        /// </summary>
        [StringLength(LocationLockConsts.MaxGatewayIdLength)]
        public string? GatewayId { get; set; }

        /// <summary>
        /// 料位检测 Tag
        /// </summary>
        [StringLength(LocationLockConsts.MaxTagLength)]
        public string? MaterialPresenceTag { get; set; }

        /// <summary>
        /// 允许入叉就绪 Tag
        /// </summary>
        [StringLength(LocationLockConsts.MaxTagLength)]
        public string? InterlockReadyTag { get; set; }

        /// <summary>
        /// 超时等待阈值（秒）
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// 备注描述
        /// </summary>
        [StringLength(512)]
        public string? Description { get; set; }
    }

    /// <summary>
    /// 库位 PLC 联锁配置分页查询条件
    /// </summary>
    public class GetLocationPlcConfigListInput : PagedAndSortedResultRequestDto
    {
        /// <summary>
        /// 关键字搜索（库位或网关编码）
        /// </summary>
        public string? Filter { get; set; }

        /// <summary>
        /// 启用状态筛选
        /// </summary>
        public bool? IsEnabled { get; set; }
    }
}
