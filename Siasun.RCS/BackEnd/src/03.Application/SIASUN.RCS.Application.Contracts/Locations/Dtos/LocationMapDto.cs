using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace SIASUN.RCS.Locations.Dtos
{
    /// <summary>
    /// 库位与 AGV 地图点位映射数据传输对象
    /// </summary>
    public class LocationMapDto : FullAuditedEntityDto<Guid>
    {
        /// <summary>
        /// 业务库位/机台编码
        /// </summary>
        public string LocationCode { get; set; } = string.Empty;

        /// <summary>
        /// 点位业务名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// AGV 地图站点编码
        /// </summary>
        public string StationCode { get; set; } = string.Empty;

        /// <summary>
        /// 进站前置引导点/停止线点位
        /// </summary>
        public string? PreDockStationCode { get; set; }

        /// <summary>
        /// 地图编码
        /// </summary>
        public string? MapCode { get; set; }

        /// <summary>
        /// 对接姿态角（度）
        /// </summary>
        public double? Heading { get; set; }

        /// <summary>
        /// 所属工艺区域
        /// </summary>
        public string? Area { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 描述说明
        /// </summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// 创建点位映射输入 DTO
    /// </summary>
    public class CreateLocationMapDto
    {
        /// <summary>
        /// 业务库位编码
        /// </summary>
        [Required]
        [StringLength(LocationMapConsts.MaxLocationCodeLength)]
        public string LocationCode { get; set; } = string.Empty;

        /// <summary>
        /// 点位显示名称
        /// </summary>
        [Required]
        [StringLength(LocationMapConsts.MaxNameLength)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// AGV 地图站点编码
        /// </summary>
        [Required]
        [StringLength(LocationMapConsts.MaxStationCodeLength)]
        public string StationCode { get; set; } = string.Empty;

        /// <summary>
        /// 进站前置引导点/停止线点位
        /// </summary>
        [StringLength(LocationMapConsts.MaxStationCodeLength)]
        public string? PreDockStationCode { get; set; }

        /// <summary>
        /// 地图编码
        /// </summary>
        [StringLength(LocationMapConsts.MaxMapCodeLength)]
        public string? MapCode { get; set; }

        /// <summary>
        /// 对接姿态角（度）
        /// </summary>
        public double? Heading { get; set; }

        /// <summary>
        /// 所属工艺区域
        /// </summary>
        [StringLength(LocationMapConsts.MaxAreaLength)]
        public string? Area { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 描述说明
        /// </summary>
        [StringLength(LocationMapConsts.MaxDescriptionLength)]
        public string? Description { get; set; }
    }

    /// <summary>
    /// 更新点位映射输入 DTO
    /// </summary>
    public class UpdateLocationMapDto
    {
        /// <summary>
        /// 点位显示名称
        /// </summary>
        [Required]
        [StringLength(LocationMapConsts.MaxNameLength)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// AGV 地图站点编码
        /// </summary>
        [Required]
        [StringLength(LocationMapConsts.MaxStationCodeLength)]
        public string StationCode { get; set; } = string.Empty;

        /// <summary>
        /// 进站前置引导点/停止线点位
        /// </summary>
        [StringLength(LocationMapConsts.MaxStationCodeLength)]
        public string? PreDockStationCode { get; set; }

        /// <summary>
        /// 地图编码
        /// </summary>
        [StringLength(LocationMapConsts.MaxMapCodeLength)]
        public string? MapCode { get; set; }

        /// <summary>
        /// 对接姿态角（度）
        /// </summary>
        public double? Heading { get; set; }

        /// <summary>
        /// 所属工艺区域
        /// </summary>
        [StringLength(LocationMapConsts.MaxAreaLength)]
        public string? Area { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 描述说明
        /// </summary>
        [StringLength(LocationMapConsts.MaxDescriptionLength)]
        public string? Description { get; set; }
    }

    /// <summary>
    /// 点位映射分页查询输入 DTO
    /// </summary>
    public class GetLocationMapListInput : PagedAndSortedResultRequestDto
    {
        /// <summary>
        /// 过滤关键字（匹配 LocationCode 或 Name 或 StationCode）
        /// </summary>
        public string? Filter { get; set; }

        /// <summary>
        /// 所属区域过滤
        /// </summary>
        public string? Area { get; set; }

        /// <summary>
        /// 启用状态过滤
        /// </summary>
        public bool? IsEnabled { get; set; }
    }
}
