using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Hardware
{
    /// <summary>
    /// 工业硬件网关适配器注册表实现
    /// 集中管理已接入的现场设备插件，支持安全回退机制
    /// </summary>
    public class HardwareGateRegistry : IHardwareGateRegistry, ISingletonDependency
    {
        private readonly ConcurrentDictionary<string, IHardwareGate> _gates = new(StringComparer.OrdinalIgnoreCase);
        private readonly ILogger<HardwareGateRegistry> _logger;

        public HardwareGateRegistry(
            IEnumerable<IHardwareGate> gates,
            ILogger<HardwareGateRegistry> logger)
        {
            _logger = logger;

            foreach (var gate in gates)
            {
                Register(gate);
            }
        }

        /// <inheritdoc />
        public void Register(IHardwareGate gate)
        {
            Check.NotNull(gate, nameof(gate));
            Check.NotNullOrWhiteSpace(gate.GateType, nameof(gate.GateType));

            _gates[gate.GateType] = gate;
            _logger.LogInformation("已注册工业硬件网关适配器: [{GateType}] -> {ImplementationType}",
                gate.GateType, gate.GetType().Name);
        }

        /// <inheritdoc />
        public IHardwareGate GetGate(string gateType)
        {
            Check.NotNullOrWhiteSpace(gateType, nameof(gateType));

            if (_gates.TryGetValue(gateType, out var gate))
            {
                return gate;
            }

            _logger.LogWarning("未找到针对硬件网关类型 [{GateType}] 的专属适配器，尝试回退至 Mock 适配器", gateType);

            if (_gates.TryGetValue("Mock", out var fallbackGate))
            {
                return fallbackGate;
            }

            throw new BusinessException(code: "HARDWARE_GATE_NOT_FOUND", message: $"未找到已注册的硬件适配器 [{gateType}] 且缺少默认 Mock 兜底适配器。");
        }

        /// <inheritdoc />
        public bool TryGetGate(string gateType, out IHardwareGate? gate)
        {
            return _gates.TryGetValue(gateType, out gate);
        }

        /// <inheritdoc />
        public IReadOnlyCollection<string> GetAllRegisteredGateTypes()
        {
            return _gates.Keys.ToArray();
        }
    }
}
