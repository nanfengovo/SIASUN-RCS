using System.Collections.Generic;

namespace SIASUN.RCS.Hardware
{
    /// <summary>
    /// 工业硬件网关适配器注册表契约
    /// 管理现场各类硬件插件（风淋门、机台对齐、双臂防撞、光电传感器）并按类别或设备解析
    /// </summary>
    public interface IHardwareGateRegistry
    {
        /// <summary>
        /// 注册硬件网关适配器
        /// </summary>
        /// <param name="gate">硬件网关实例</param>
        void Register(IHardwareGate gate);

        /// <summary>
        /// 根据网关类别解析适配器（如未找到且允许降级，返回默认 Mock 适配器）
        /// </summary>
        /// <param name="gateType">网关类型标识（如 "AirShowerDoor"）</param>
        /// <returns>匹配的硬件适配器</returns>
        IHardwareGate GetGate(string gateType);

        /// <summary>
        /// 尝试获取已注册的硬件适配器
        /// </summary>
        /// <param name="gateType">网关类型标识</param>
        /// <param name="gate">解析得到的网关实例</param>
        /// <returns>是否存在匹配的网关</returns>
        bool TryGetGate(string gateType, out IHardwareGate? gate);

        /// <summary>
        /// 获取所有已注册的网关类型清单
        /// </summary>
        /// <returns>已注册的类型列表</returns>
        IReadOnlyCollection<string> GetAllRegisteredGateTypes();
    }
}
