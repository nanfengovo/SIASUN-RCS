using System;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Ports
{
    /// <summary>
    /// 智能立体库（Stocker / STKC / WMS）交互出站端口契约（六边形架构）
    /// 涵盖蒙莹 STKC REST 接口与 Mica WMS SOAP 接口对接
    /// </summary>
    public interface IStockerAdapter : ITransientDependency
    {
        /// <summary>
        /// 查询立体库出入库输送机端口状态
        /// </summary>
        /// <param name="stockerCode">立库编号</param>
        /// <param name="portCode">端口号（例如 P01 / P02）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>端口物理状态（就绪、已就位、占用、故障）</returns>
        Task<StockerPortStatus> QueryPortStatusAsync(
            string stockerCode,
            string portCode,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// AGV 到达立库接驳口，通知立库准备接驳
        /// </summary>
        Task<bool> NotifyArrivalAsync(
            string stockerCode,
            string portCode,
            string agvCode,
            string carrierCode,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// AGV 完成取/放载具，通知立库握手完结
        /// </summary>
        Task<bool> NotifyTransferCompleteAsync(
            string stockerCode,
            string portCode,
            string agvCode,
            string carrierCode,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 立库接驳口物理状态
    /// </summary>
    public enum StockerPortStatus
    {
        Unknown = 0,
        ReadyForPick = 1,
        ReadyForPut = 2,
        Busy = 3,
        Disabled = 4
    }
}
