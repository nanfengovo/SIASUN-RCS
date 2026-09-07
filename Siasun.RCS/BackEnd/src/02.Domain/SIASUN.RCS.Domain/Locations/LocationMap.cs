using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace SIASUN.RCS.Locations
{
    /// <summary>
    /// 业务库位/机台与 AGV 地图物理站点映射聚合根
    /// 将上游业务库位编码（LocationCode / MachinePoint）解耦映射为底层机器人实际导航点位（StationCode / PreDockStationCode）
    /// </summary>
    public class LocationMap : FullAuditedAggregateRoot<Guid>
    {
        /// <summary>
        /// 业务库位/机台编码（全局唯一业务索引，如 "STK-IN-01", "EQP-WB-08"）
        /// 与上游搬运任务及 LocationLock 互斥范围保持一致
        /// </summary>
        public string LocationCode { get; private set; } = string.Empty;

        /// <summary>
        /// 点位业务名称或别名（如 "1号注塑机上料口"）
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// AGV/TM 导航地图中的实际物理停靠点位号（如 "1024", "P201"）
        /// 最终下发给底层新松车载调度系统报文中的关键导航参数
        /// </summary>
        public string StationCode { get; private set; } = string.Empty;

        /// <summary>
        /// 进站前置引导点/干涉区停止线点位（可选，如 "1020"，用于进入机台或立库前的光电联锁等待点）
        /// </summary>
        public string? PreDockStationCode { get; private set; }

        /// <summary>
        /// 所属电子地图编码或楼层标识（可选，如 "MAP_FAB_1F"）
        /// </summary>
        public string? MapCode { get; private set; }

        /// <summary>
        /// 车辆停靠对接姿态角度（可选，单位：度，如 90.0, 180.0）
        /// </summary>
        public double? Heading { get; private set; }

        /// <summary>
        /// 所属工艺车间或作业区域（可选，如 "CleanRoom", "Warehouse", "SMT"）
        /// </summary>
        public string? Area { get; private set; }

        /// <summary>
        /// 该映射是否启用（未启用时调度引擎解析将视为无效点位）
        /// </summary>
        public bool IsEnabled { get; private set; } = true;

        /// <summary>
        /// 描述说明或运维备注
        /// </summary>
        public string? Description { get; private set; }

        /// <summary>
        /// EF Core 所需受保护无参构造函数
        /// </summary>
        protected LocationMap()
        {
        }

        /// <summary>
        /// 创建新的点位映射实体
        /// </summary>
        /// <param name="id">实体主键</param>
        /// <param name="locationCode">业务库位编码</param>
        /// <param name="name">业务显示名称</param>
        /// <param name="stationCode">AGV 地图站点编码</param>
        /// <param name="preDockStationCode">前置引导点/停止线点位</param>
        /// <param name="mapCode">地图编码</param>
        /// <param name="heading">对接姿态角度</param>
        /// <param name="area">所属工艺区域</param>
        /// <param name="description">描述说明</param>
        /// <param name="isEnabled">是否启用（默认 true）</param>
        public LocationMap(
            Guid id,
            string locationCode,
            string name,
            string stationCode,
            string? preDockStationCode = null,
            string? mapCode = null,
            double? heading = null,
            string? area = null,
            string? description = null,
            bool isEnabled = true) : base(id)
        {
            LocationCode = Check.NotNullOrWhiteSpace(locationCode, nameof(locationCode), maxLength: LocationMapConsts.MaxLocationCodeLength);
            Name = Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: LocationMapConsts.MaxNameLength);
            StationCode = Check.NotNullOrWhiteSpace(stationCode, nameof(stationCode), maxLength: LocationMapConsts.MaxStationCodeLength);
            PreDockStationCode = Check.Length(preDockStationCode, nameof(preDockStationCode), LocationMapConsts.MaxStationCodeLength);
            MapCode = Check.Length(mapCode, nameof(mapCode), LocationMapConsts.MaxMapCodeLength);
            Heading = heading;
            Area = Check.Length(area, nameof(area), LocationMapConsts.MaxAreaLength);
            Description = Check.Length(description, nameof(description), LocationMapConsts.MaxDescriptionLength);
            IsEnabled = isEnabled;
        }

        /// <summary>
        /// 更新点位映射配置属性
        /// </summary>
        public void UpdateDetails(
            string name,
            string stationCode,
            string? preDockStationCode,
            string? mapCode,
            double? heading,
            string? area,
            string? description)
        {
            Name = Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: LocationMapConsts.MaxNameLength);
            StationCode = Check.NotNullOrWhiteSpace(stationCode, nameof(stationCode), maxLength: LocationMapConsts.MaxStationCodeLength);
            PreDockStationCode = Check.Length(preDockStationCode, nameof(preDockStationCode), LocationMapConsts.MaxStationCodeLength);
            MapCode = Check.Length(mapCode, nameof(mapCode), LocationMapConsts.MaxMapCodeLength);
            Heading = heading;
            Area = Check.Length(area, nameof(area), LocationMapConsts.MaxAreaLength);
            Description = Check.Length(description, nameof(description), LocationMapConsts.MaxDescriptionLength);
        }

        /// <summary>
        /// 启用点位映射
        /// </summary>
        public void Enable()
        {
            IsEnabled = true;
        }

        /// <summary>
        /// 禁用点位映射
        /// </summary>
        public void Disable()
        {
            IsEnabled = false;
        }
    }
}
