using Microsoft.Extensions.DependencyInjection;
using SIASUN.RCS.Hardware;
using Volo.Abp.Modularity;

namespace SIASUN.RCS.Adapters.Hardware
{
    /// <summary>
    /// SIASUN RCS 工业硬件与现场设备适配器模块
    /// 遵循《AGENTS.md》铁律 4：提供风淋门、机台对齐、双臂防撞、光电传感器等插件化实现
    /// </summary>
    [DependsOn(
        typeof(RCSDomainModule)
    )]
    public class RCSAdaptersHardwareModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            // 注册各硬件网关实现到 DI，以便在 HardwareGateRegistry 启动时自动注入聚合
            context.Services.AddTransient<IHardwareGate, MockHardwareGateAdapter>();
            context.Services.AddTransient<IHardwareGate, CleanroomAirShowerGateAdapter>();
            context.Services.AddTransient<IHardwareGate, TwinArmInterlockGateAdapter>();
        }
    }
}
