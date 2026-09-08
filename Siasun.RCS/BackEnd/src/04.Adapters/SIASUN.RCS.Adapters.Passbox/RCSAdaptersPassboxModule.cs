using Microsoft.Extensions.DependencyInjection;
using SIASUN.RCS.Hardware;
using SIASUN.RCS.Ports;
using Volo.Abp.Modularity;

namespace SIASUN.RCS.Adapters.Passbox
{
    /// <summary>
    /// 洁净区传递窗（晖哲 8 接口双门互锁）与风淋门硬件适配模块
    /// </summary>
    [DependsOn(typeof(RCSDomainModule))]
    public class RCSAdaptersPassboxModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<IPassboxAdapter, HuizhePassboxAdapter>();
            context.Services.AddTransient<HuizhePassboxAdapter>();
            context.Services.AddTransient<IHardwareGate, CleanroomAirShowerGateAdapter>();
            context.Services.AddTransient<IHardwareGate, MockHardwareGateAdapter>();
        }
    }
}
