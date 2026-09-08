using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Hardware;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Adapters.Passbox
{
    /// <summary>
    /// Mock 模拟硬件网关适配器
    /// 遵循《AGENTS.md》铁律 4 与规范三.5：纯插件化隔离，支持离线运行、仿真模拟与单元测试环境兜底
    /// </summary>
    public class MockHardwareGateAdapter : IHardwareGate, ITransientDependency
    {
        private readonly ILogger<MockHardwareGateAdapter> _logger;

        /// <inheritdoc />
        public string GateType => "Mock";

        /// <summary>
        /// 是否强制让下一次条件校验返回失败（用于测试异常分支）
        /// </summary>
        public bool SimulateConditionFailure { get; set; }

        /// <summary>
        /// 是否强制让动作执行失败
        /// </summary>
        public bool SimulateActionFailure { get; set; }

        public MockHardwareGateAdapter(ILogger<MockHardwareGateAdapter> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public Task<HardwareGateResult> CheckConditionAsync(
            HardwareGateContext context,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("MockHardwareGate 校验前置条件: Device={Device}, Location={Location}",
                context.DeviceId, context.LocationCode);

            if (SimulateConditionFailure)
            {
                return Task.FromResult(HardwareGateResult.Failed(
                    "MOCK_CONDITION_BLOCKED",
                    $"模拟硬件条件校验未通过: DeviceId={context.DeviceId}"));
            }

            return Task.FromResult(HardwareGateResult.Success(
                $"模拟硬件前置条件满足: DeviceId={context.DeviceId}"));
        }

        /// <inheritdoc />
        public Task<HardwareGateResult> ExecuteActionAsync(
            HardwareGateContext context,
            string actionName,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("MockHardwareGate 执行动作: Action={Action}, Device={Device}",
                actionName, context.DeviceId);

            if (SimulateActionFailure)
            {
                return Task.FromResult(HardwareGateResult.Failed(
                    "MOCK_ACTION_FAILED",
                    $"模拟动作执行失败: Action={actionName}, Device={context.DeviceId}"));
            }

            return Task.FromResult(HardwareGateResult.Success(
                $"模拟动作执行成功: Action={actionName}, Device={context.DeviceId}"));
        }

        /// <inheritdoc />
        public Task<bool> WaitForSignalAsync(
            HardwareGateContext context,
            string expectedSignal,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("MockHardwareGate 等待就绪信号: Signal={Signal}, Timeout={Timeout}ms",
                expectedSignal, timeout.TotalMilliseconds);

            return Task.FromResult(true);
        }
    }
}
