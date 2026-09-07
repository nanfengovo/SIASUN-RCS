using System.Threading;
using System.Threading.Tasks;

namespace SIASUN.RCS.Locations
{
    /// <summary>
    /// 业务库位与 AGV 地图站点高频转换解析器领域契约
    /// 为调度引擎与 TM 适配器提供亚毫秒级的内存字典点位转换与引导点查询
    /// </summary>
    public interface ILocationMapResolver
    {
        /// <summary>
        /// 将业务库位编码解析为 AGV/TM 地图站点编码
        /// </summary>
        /// <param name="locationCode">业务库位编码（如 "STK-IN-01"）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>AGV 地图站点编码（如 "1024"），若未找到或已禁用则返回 null</returns>
        Task<string?> ResolveStationCodeAsync(string locationCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取完整的库位地图映射详情（含前置引导点、姿态角、地图号）
        /// </summary>
        /// <param name="locationCode">业务库位编码</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>映射详情值对象，未找到或已禁用返回 null</returns>
        Task<LocationMapInfo?> GetMappingAsync(string locationCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// 主动刷新点位映射本地内存高速缓存
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        Task RefreshCacheAsync(CancellationToken cancellationToken = default);
    }
}
