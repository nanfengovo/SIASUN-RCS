namespace SIASUN.RCS.Locations
{
    /// <summary>
    /// 库位映射高频只读值对象（用于调度微内核快速寻址与缓存）
    /// </summary>
    public class LocationMapInfo
    {
        /// <summary>
        /// 业务库位编码
        /// </summary>
        public string LocationCode { get; }

        /// <summary>
        /// AGV/TM 导航站点编码
        /// </summary>
        public string StationCode { get; }

        /// <summary>
        /// 进站前置引导点/停止线点位
        /// </summary>
        public string? PreDockStationCode { get; }

        /// <summary>
        /// 所属地图编码
        /// </summary>
        public string? MapCode { get; }

        /// <summary>
        /// 对接姿态角（度）
        /// </summary>
        public double? Heading { get; }

        /// <summary>
        /// 所属工艺区域
        /// </summary>
        public string? Area { get; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; }

        public LocationMapInfo(
            string locationCode,
            string stationCode,
            string? preDockStationCode = null,
            string? mapCode = null,
            double? heading = null,
            string? area = null,
            bool isEnabled = true)
        {
            LocationCode = locationCode;
            StationCode = stationCode;
            PreDockStationCode = preDockStationCode;
            MapCode = mapCode;
            Heading = heading;
            Area = area;
            IsEnabled = isEnabled;
        }
    }
}
