using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Ports;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Adapters.Stocker
{
    /// <summary>
    /// 智能立体库复合适配器（实现 IStockerAdapter 六边形出站端口）
    /// 支持统一调度并在蒙莹 STKC REST 与 Mica WMS SOAP 协议间平滑切换
    /// </summary>
    public class StockerCompositeAdapter : IStockerAdapter, ITransientDependency
    {
        private readonly IMengyingStkcClient _stkcClient;
        private readonly IMicaWmsSoapClient _wmsClient;
        private readonly ILogger<StockerCompositeAdapter> _logger;

        public StockerCompositeAdapter(
            IMengyingStkcClient stkcClient,
            IMicaWmsSoapClient wmsClient,
            ILogger<StockerCompositeAdapter> logger)
        {
            _stkcClient = stkcClient;
            _wmsClient = wmsClient;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<StockerPortStatus> QueryPortStatusAsync(
            string stockerCode,
            string portCode,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("立体库适配器查询端口状态: Stocker={Stocker}, Port={Port}", stockerCode, portCode);
            return await _stkcClient.QueryPortStatusAsync(stockerCode, portCode, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> NotifyArrivalAsync(
            string stockerCode,
            string portCode,
            string agvCode,
            string carrierCode,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("立体库适配器通知 AGV 到达: Stocker={Stocker}, Port={Port}, AGV={Agv}, Carrier={Carrier}",
                stockerCode, portCode, agvCode, carrierCode);

            // 1. 同步通知 STKC 接驳就位
            var stkcOk = await _stkcClient.NotifyArrivalAsync(stockerCode, portCode, agvCode, carrierCode, cancellationToken);

            // 2. 联动通知 Mica WMS
            await _wmsClient.ConfirmCarrierTransferAsync($"{stockerCode}_{portCode}", cancellationToken);

            return stkcOk;
        }

        /// <inheritdoc />
        public async Task<bool> NotifyTransferCompleteAsync(
            string stockerCode,
            string portCode,
            string agvCode,
            string carrierCode,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("立体库适配器通知交接完成: Stocker={Stocker}, Port={Port}, AGV={Agv}, Carrier={Carrier}",
                stockerCode, portCode, agvCode, carrierCode);

            var stkcOk = await _stkcClient.NotifyTransferCompleteAsync(stockerCode, portCode, agvCode, carrierCode, cancellationToken);
            await _wmsClient.NotifyTaskCompletedAsync($"{stockerCode}_{portCode}", carrierCode, cancellationToken);

            return stkcOk;
        }
    }
}
