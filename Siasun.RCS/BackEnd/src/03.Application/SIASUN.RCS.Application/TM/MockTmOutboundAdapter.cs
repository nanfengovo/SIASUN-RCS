using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Tasks;
using Volo.Abp;

namespace SIASUN.RCS.TM
{
    /// <summary>
    /// TM 出站适配器实现（生成 TM 报文流水号、自动绑定 TaskSerialRegistry 并下发指令）
    /// </summary>
    public class MockTmOutboundAdapter : ITmOutboundAdapter
    {
        private readonly ITaskSerialRegistry _serialRegistry;
        private readonly ILogger<MockTmOutboundAdapter> _logger;

        /// <summary>
        /// 构造函数注入序列号注册中心与日志
        /// </summary>
        public MockTmOutboundAdapter(
            ITaskSerialRegistry serialRegistry,
            ILogger<MockTmOutboundAdapter> logger)
        {
            _serialRegistry = serialRegistry;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<TmDispatchResult> DispatchLegAsync(TmDispatchContext context, CancellationToken cancellationToken = default)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNullOrWhiteSpace(context.TaskCode, nameof(context.TaskCode));
            Check.NotNullOrWhiteSpace(context.Leg, nameof(context.Leg));

            // 生成具备现场业务可读性的全局唯一 TM 序列号（如 TM_20260908_FETCH_a1b2c3）
            var randomSuffix = Guid.NewGuid().ToString("N")[..6];
            var tmSerial = $"TM_{DateTime.UtcNow:yyyyMMddHHmmss}_{context.Leg.ToUpperInvariant()}_{randomSuffix}";

            // 1. 严格在注册表中建立持久化映射（选项 B），准备应对后续异步回调与断电自愈
            await _serialRegistry.RegisterAsync(
                context.TaskId,
                context.TaskCode,
                tmSerial,
                context.Leg,
                context.StepIndex,
                context.WaitingEvent,
                context.VehicleCode,
                cancellationToken);

            _logger.LogInformation("已向 TM 派发航段指令: [TmSerial={TmSerial}] -> [Task={TaskCode}, Leg={Leg}, AGV={VehicleCode}, OptionCode={OptionCode}]",
                tmSerial, context.TaskCode, context.Leg, context.VehicleCode, context.OptionCode ?? "None");

            return TmDispatchResult.Succeeded(tmSerial);
        }
    }
}
