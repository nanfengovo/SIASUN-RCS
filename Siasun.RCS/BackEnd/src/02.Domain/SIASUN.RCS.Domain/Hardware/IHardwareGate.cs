using System;
using System.Threading;
using System.Threading.Tasks;

namespace SIASUN.RCS.Hardware
{
    /// <summary>
    /// 六边形架构工业硬件端口契约
    /// 遵循《AGENTS.md》铁律 4：严格将 PLC/门禁/互锁等现场硬件交互隔离在 IHardwareGate 之后，禁止侵入核心调度内核
    /// </summary>
    public interface IHardwareGate
    {
        /// <summary>
        /// 硬件网关类别标识（如 "AirShowerDoor", "TwinArmInterlock", "SlotInterlock", "Mock"）
        /// </summary>
        string GateType { get; }

        /// <summary>
        /// 校验现场硬件前置条件（如门禁是否已解锁、双臂是否在安全位、库位光电是否无干涉）
        /// </summary>
        /// <param name="context">门禁上下文参数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>门禁条件校验结果</returns>
        Task<HardwareGateResult> CheckConditionAsync(
            HardwareGateContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 向现场硬件发送受控动作指令（如开门、关门、请求进站通行、互锁释放）
        /// </summary>
        /// <param name="context">门禁上下文参数</param>
        /// <param name="actionName">动作名称（如 "RequestEntry", "OpenDoor", "CloseDoor", "ReleaseInterlock"）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>动作执行结果</returns>
        Task<HardwareGateResult> ExecuteActionAsync(
            HardwareGateContext context,
            string actionName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步等待硬件就绪信号或状态到达（带超时保护）
        /// </summary>
        /// <param name="context">门禁上下文参数</param>
        /// <param name="expectedSignal">期望的信号状态或事件名（如 "DoorOpened", "InterlockReleased"）</param>
        /// <param name="timeout">最大超时时间</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否在超时前收到期望信号</returns>
        Task<bool> WaitForSignalAsync(
            HardwareGateContext context,
            string expectedSignal,
            TimeSpan timeout,
            CancellationToken cancellationToken = default);
    }
}
