using System;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Ports
{
    /// <summary>
    /// 洁净区传递窗 / 风淋门双门互锁设备出站端口契约（六边形架构）
    /// 涵盖晖哲 8 接口双门互锁状态机适配与洁净室风淋除尘吹扫流程
    /// </summary>
    public interface IPassboxAdapter : ITransientDependency
    {
        /// <summary>
        /// 请求开启前门（外侧门）准备进入传递
        /// </summary>
        Task<PassboxOperationResult> RequestOpenFrontDoorAsync(
            string passboxId,
            string agvCode,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 车辆进入窗体后，请求关闭前门并启动洁净自净吹扫
        /// </summary>
        Task<PassboxOperationResult> CloseAndStartPurgeAsync(
            string passboxId,
            string agvCode,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 自净吹扫完成后，请求开启后门（内侧洁净室门）放行
        /// </summary>
        Task<PassboxOperationResult> RequestOpenBackDoorAsync(
            string passboxId,
            string agvCode,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 车辆完全驶离后，通知传递窗关闭后门并复位互锁状态机
        /// </summary>
        Task<PassboxOperationResult> NotifyExitedAndResetAsync(
            string passboxId,
            string agvCode,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 传递窗操作执行结果
    /// </summary>
    public record PassboxOperationResult(
        bool Success,
        string State,
        string? ErrorMessage = null);
}
