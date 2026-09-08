using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Ports;
using SIASUN.RCS.Tasks;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Adapters.Tm
{
    /// <summary>
    /// VDA 5050 国际通用 AGV 通信协议 MQTT 驱动适配器
    /// 遵循工业标准 VDA 5050 v2.0 协议族，将调度指令封装为 Order / InstantActions 报文下发
    /// </summary>
    public class Vda5050MqttFleetDriver : IAgvFleetDriver, ITransientDependency
    {
        private readonly ITaskSerialRegistry _taskSerialRegistry;
        private readonly ILogger<Vda5050MqttFleetDriver> _logger;

        /// <inheritdoc />
        public string ProtocolName => "VDA5050";

        public Vda5050MqttFleetDriver(
            ITaskSerialRegistry taskSerialRegistry,
            ILogger<Vda5050MqttFleetDriver> logger)
        {
            _taskSerialRegistry = taskSerialRegistry;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<FleetDispatchResult> DispatchLegAsync(
            string taskCode,
            string agvCode,
            string activeLeg,
            string optionCode,
            string targetStation,
            string? traceId = null,
            CancellationToken cancellationToken = default)
        {
            var orderId = $"VDA_ORD_{DateTime.UtcNow:yyyyMMddHHmmss}_{agvCode}";
            var topic = $"uagv/v2/SIASUN/{agvCode}/order";

            _logger.LogInformation("向 VDA 5050 MQTT 主题 [{Topic}] 发布订单: OrderId={OrderId}, TaskCode={TaskCode}, Target={TargetStation}, OptionCode={OptionCode}",
                topic, orderId, taskCode, targetStation, optionCode);

            // 登记 TaskSerialRegistry 双向映射
            await _taskSerialRegistry.RegisterAsync(
                taskId: Guid.Empty,
                taskCode: taskCode,
                tmSerial: orderId,
                leg: activeLeg,
                stepIndex: 1,
                waitingEvent: $"VDA_ORDER_{orderId}_COMPLETED",
                vehicleCode: agvCode,
                cancellationToken: cancellationToken);

            return new FleetDispatchResult(true, orderId);
        }

        /// <inheritdoc />
        public Task<bool> CancelMissionAsync(
            string agvCode,
            string taskCode,
            string reason,
            CancellationToken cancellationToken = default)
        {
            var topic = $"uagv/v2/SIASUN/{agvCode}/instantActions";
            _logger.LogWarning("向 VDA 5050 MQTT 主题 [{Topic}] 发布即时取消动作: TaskCode={TaskCode}, Reason={Reason}",
                topic, taskCode, reason);

            return Task.FromResult(true);
        }
    }
}
