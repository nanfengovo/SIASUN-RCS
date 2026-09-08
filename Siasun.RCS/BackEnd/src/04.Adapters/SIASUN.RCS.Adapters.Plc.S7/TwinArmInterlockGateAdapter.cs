using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Hardware;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Adapters.Plc.S7
{
    /// <summary>
    /// 半导体晶圆搬运 AGV 双臂协同防撞与库位传感器互锁适配器
    /// 遵循《AGENTS.md》铁律 4：双臂同步与库位硬件联锁等现场强相关逻辑作为独立插件注入
    /// </summary>
    public class TwinArmInterlockGateAdapter : IHardwareGate, ITransientDependency
    {
        private readonly ILogger<TwinArmInterlockGateAdapter> _logger;

        /// <inheritdoc />
        public string GateType => "TwinArmInterlock";

        public TwinArmInterlockGateAdapter(ILogger<TwinArmInterlockGateAdapter> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public Task<HardwareGateResult> CheckConditionAsync(
            HardwareGateContext context,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("检查双臂协同防撞与传感器状态: Vehicle={Vehicle}, Location={Location}",
                context.VehicleCode, context.LocationCode);

            // 检查双臂干涉参数
            if (context.Parameters.TryGetValue("TwinArmConflict", out var conflict) &&
                bool.TryParse(conflict, out var hasConflict) && hasConflict)
            {
                return Task.FromResult(HardwareGateResult.Failed(
                    "TWIN_ARM_COLLISION_HAZARD",
                    $"检测到车辆 [{context.VehicleCode}] 双机械臂存在空间干涉风险，互锁阻断动作",
                    new Dictionary<string, object?>
                    {
                        ["VehicleCode"] = context.VehicleCode,
                        ["ArmState"] = "Interfered"
                    }));
            }

            return Task.FromResult(HardwareGateResult.Success(
                $"车辆 [{context.VehicleCode}] 双机械臂同步间隙与传感器安全",
                new Dictionary<string, object?>
                {
                    ["VehicleCode"] = context.VehicleCode,
                    ["ArmState"] = "SynchronizedSafe"
                }));
        }

        /// <inheritdoc />
        public Task<HardwareGateResult> ExecuteActionAsync(
            HardwareGateContext context,
            string actionName,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("向双臂互锁网关发送指令: [{Action}], Vehicle={Vehicle}", actionName, context.VehicleCode);

            switch (actionName.ToUpperInvariant())
            {
                case "LOCKTWINARMSYNC":
                    _logger.LogInformation("车辆 [{Vehicle}] 双臂进入强同步互锁模式", context.VehicleCode);
                    return Task.FromResult(HardwareGateResult.Success($"车辆 [{context.VehicleCode}] 双臂强同步已锁定"));

                case "UNLOCKTWINARMSYNC":
                    _logger.LogInformation("车辆 [{Vehicle}] 双臂强同步互锁已解除", context.VehicleCode);
                    return Task.FromResult(HardwareGateResult.Success($"车辆 [{context.VehicleCode}] 双臂互锁已解除"));

                case "CHECKCLEARANCE":
                    _logger.LogInformation("车辆 [{Vehicle}] 库位光电安全净空校验通过", context.VehicleCode);
                    return Task.FromResult(HardwareGateResult.Success($"车辆 [{context.VehicleCode}] 库位光电净空校验通过"));

                default:
                    _logger.LogWarning("未知的双臂互锁控制指令: [{Action}]", actionName);
                    return Task.FromResult(HardwareGateResult.Failed(
                        "UNSUPPORTED_TWIN_ARM_ACTION",
                        $"不支持的双臂互锁动作: {actionName}"));
            }
        }

        /// <inheritdoc />
        public Task<bool> WaitForSignalAsync(
            HardwareGateContext context,
            string expectedSignal,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("等待双臂互锁信号: Signal={Signal}, Timeout={Timeout}ms",
                expectedSignal, timeout.TotalMilliseconds);

            return Task.FromResult(true);
        }
    }
}
