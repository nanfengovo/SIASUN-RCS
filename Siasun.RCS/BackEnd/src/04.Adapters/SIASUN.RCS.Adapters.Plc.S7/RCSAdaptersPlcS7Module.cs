using Microsoft.Extensions.DependencyInjection;
using SIASUN.RCS.Hardware;
using SIASUN.RCS.Ports;
using Volo.Abp.Modularity;

namespace SIASUN.RCS.Adapters.Plc.S7
{
    /// <summary>
    /// 西门子 S7 工业 PLC 硬件与传感器适配模块
    /// </summary>
    [DependsOn(typeof(RCSDomainModule))]
    public class RCSAdaptersPlcS7Module : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<S7TagCache>();
            context.Services.AddHostedService<S7BatchReadWorker>();

            context.Services.AddTransient<IPlcHardwareGate, SiemensS7PlcHardwareGate>();
            context.Services.AddTransient<IHardwareGate, SiemensS7PlcHardwareGate>();
            context.Services.AddTransient<IHardwareGate, TwinArmInterlockGateAdapter>();
        }
    }
}
