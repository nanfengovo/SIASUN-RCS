using System;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Ports
{
    /// <summary>
    /// AGV 移动机器人车队驱动端口契约（六边形架构出站端口）
    /// 统一抽象新松底层 TM 控制协议与国际标准化 VDA 5050 协议
    /// </summary>
    public interface IAgvFleetDriver : ITransientDependency
    {
        /// <summary>
        /// 驱动协议代号（例如 "SIASUN_TM", "VDA5050"）
        /// </summary>
        string ProtocolName { get; }

        /// <summary>
        /// 向移动机器人下发程段路径搬运任务
        /// </summary>
        /// <param name="taskCode">RCS 内部任务编号</param>
        /// <param name="agvCode">目标车辆代号</param>
        /// <param name="activeLeg">当前程段（Fetch / Put / Transit）</param>
        /// <param name="optionCode">32位位图 OptionCode 字符串</param>
        /// <param name="targetStation">目标点位代码</param>
        /// <param name="traceId">全链路追踪标识</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>派发结果（包含底层 TM 序列号与成功状态）</returns>
        Task<FleetDispatchResult> DispatchLegAsync(
            string taskCode,
            string agvCode,
            string activeLeg,
            string optionCode,
            string targetStation,
            string? traceId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 中止或取消车载在途任务
        /// </summary>
        /// <param name="agvCode">车辆代号</param>
        /// <param name="taskCode">任务编号</param>
        /// <param name="reason">取消原因</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>取消是否成功</returns>
        Task<bool> CancelMissionAsync(
            string agvCode,
            string taskCode,
            string reason,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 车队指令派发响应结果
    /// </summary>
    public record FleetDispatchResult(
        bool Success,
        string? ExternalSerial,
        string? ErrorMessage = null);
}
