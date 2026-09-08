using System;
using System.Threading;
using System.Threading.Tasks;
using SIASUN.RCS.Hardware;

namespace SIASUN.RCS.Ports
{
    /// <summary>
    /// PLC 硬件互锁端口契约（六边形架构出站端口）
    /// 遵循规范与 IHardwareGate 对齐，涵盖西门子 S7 批量读取、槽位偏移与传感器白名单核验
    /// </summary>
    public interface IPlcHardwareGate : IHardwareGate
    {
    }
}
