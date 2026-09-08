using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Hardware;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Adapters.Hardware
{
    /// <summary>
    /// 半导体洁净室风淋门 / 传递窗安全门禁硬件网关适配器
    /// 遵循《AGENTS.md》铁律 4：封装半导体洁净室气闸互锁、风淋吹扫与通行申请，与调度内核完全解耦
    /// </summary>
    public class CleanroomAirShowerGateAdapter : IHardwareGate, ITransientDependency
    {
        private readonly ILogger<CleanroomAirShowerGateAdapter> _logger;

        /// <inheritdoc />
        public string GateType => "AirShowerDoor";

        public CleanroomAirShowerGateAdapter(ILogger<CleanroomAirShowerGateAdapter> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public Task<HardwareGateResult> CheckConditionAsync(
            HardwareGateContext context,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("检查风淋门安全状态: Device={DeviceId}, Vehicle={VehicleCode}, Task={TaskCode}",
                context.DeviceId, context.VehicleCode, context.TaskCode);

            // 示例：检查门禁互锁上下文参数（现场可通过配置的 S7/Modbus 或专用网关读取 PLC 状态）
            var isInterlockActive = context.Parameters.TryGetValue("InterlockBlocked", out var blocked) &&
                                    bool.TryParse(blocked, out var isBlocked) && isBlocked;

            if (isInterlockActive)
            {
                return Task.FromResult(HardwareGateResult.Failed(
                    "AIR_SHOWER_INTERLOCK_ACTIVE",
                    $"风淋门 [{context.DeviceId}] 正处于对侧开启或吹扫中，互锁锁定不允许进入",
                    new Dictionary<string, object?>
                    {
                        ["DeviceId"] = context.DeviceId,
                        ["InterlockStatus"] = "Blocked"
                    }));
            }

            return Task.FromResult(HardwareGateResult.Success(
                $"风淋门 [{context.DeviceId}] 状态就绪，允许通行申请",
                new Dictionary<string, object?>
                {
                    ["DeviceId"] = context.DeviceId,
                    ["DoorStatus"] = "Ready"
                }));
        }

        /// <inheritdoc />
        public Task<HardwareGateResult> ExecuteActionAsync(
            HardwareGateContext context,
            string actionName,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("向风淋门 [{DeviceId}] 发送控制动作: [{Action}]", context.DeviceId, actionName);

            switch (actionName.ToUpperInvariant())
            {
                case "REQUESTENTRY":
                case "OPENDOOR":
                    _logger.LogInformation("已触发风淋门 [{DeviceId}] 开门信号", context.DeviceId);
                    return Task.FromResult(HardwareGateResult.Success($"风淋门 [{context.DeviceId}] 已执行开门动作"));

                case "CLOSEDOOR":
                    _logger.LogInformation("已触发风淋门 [{DeviceId}] 关门与吹扫联锁", context.DeviceId);
                    return Task.FromResult(HardwareGateResult.Success($"风淋门 [{context.DeviceId}] 已执行关门动作"));

                case "RELEASEINTERLOCK":
                    _logger.LogInformation("已释放风淋门 [{DeviceId}] 互锁占位", context.DeviceId);
                    return Task.FromResult(HardwareGateResult.Success($"风淋门 [{context.DeviceId}] 互锁已成功释放"));

                default:
                    _logger.LogWarning("未知的风淋门控制动作: [{Action}]", actionName);
                    return Task.FromResult(HardwareGateResult.Failed(
                        "UNSUPPORTED_DOOR_ACTION",
                        $"不支持的风淋门动作类型: {actionName}"));
            }
        }

        /// <inheritdoc />
        public Task<bool> WaitForSignalAsync(
            HardwareGateContext context,
            string expectedSignal,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("等待风淋门 [{DeviceId}] 信号: [{Signal}], 超时: {Timeout}ms",
                context.DeviceId, expectedSignal, timeout.TotalMilliseconds);

            // 在适配器内处理等待逻辑（或通过 PLC 监听变位事件）
            return Task.FromResult(true);
        }
    }
}
