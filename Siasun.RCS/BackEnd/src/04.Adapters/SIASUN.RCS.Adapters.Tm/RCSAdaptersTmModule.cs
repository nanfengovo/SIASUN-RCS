using Microsoft.Extensions.DependencyInjection;
using SIASUN.RCS.Ports;
using Volo.Abp.Modularity;

namespace SIASUN.RCS.Adapters.Tm
{
    /// <summary>
    /// 新松 TM 与 VDA 5050 车队通信适配器模块
    /// </summary>
    [DependsOn(typeof(RCSDomainModule))]
    public class RCSAdaptersTmModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<ISiasunTmClient, SiasunTmClient>();
            context.Services.AddTransient<IAgvFleetDriver, SiasunTmFleetDriver>();
            context.Services.AddTransient<SiasunTmFleetDriver>();
            context.Services.AddTransient<Vda5050MqttFleetDriver>();
            context.Services.AddTransient<TmCallbackGatewayParser>();
        }
    }
}
