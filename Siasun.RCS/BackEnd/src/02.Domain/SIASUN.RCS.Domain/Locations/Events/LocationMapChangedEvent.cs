using System;

namespace SIASUN.RCS.Locations.Events
{
    /// <summary>
    /// 库位点位映射变更领域事件
    /// 用于通知节点高速缓存失效与主动热重载，保证点位变更零延迟生效
    /// </summary>
    public class LocationMapChangedEvent
    {
        /// <summary>
        /// 变更涉及的业务库位编码（若为全量刷新则为 string.Empty）
        /// </summary>
        public string LocationCode { get; }

        /// <summary>
        /// 变更类型（Created, Updated, Deleted, CacheRefreshed）
        /// </summary>
        public string ChangeType { get; }

        /// <summary>
        /// 事件触发时间（UTC）
        /// </summary>
        public DateTime Timestamp { get; }

        public LocationMapChangedEvent(string locationCode, string changeType, DateTime? timestamp = null)
        {
            LocationCode = locationCode;
            ChangeType = changeType;
            Timestamp = timestamp ?? DateTime.UtcNow;
        }
    }
}
