using Microsoft.Extensions.DependencyInjection;
using SIASUN.RCS.Ports;
using Volo.Abp.Modularity;

namespace SIASUN.RCS.Adapters.Stocker
{
    /// <summary>
    /// 智能立体库（蒙莹 STKC REST + Mica WMS SOAP）适配器模块
    /// </summary>
    [DependsOn(typeof(RCSDomainModule))]
    public class RCSAdaptersStockerModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<IMengyingStkcClient, MengyingStkcClient>();
            context.Services.AddTransient<IMicaWmsSoapClient, MicaWmsSoapClient>();
            context.Services.AddTransient<IStockerAdapter, StockerCompositeAdapter>();
        }
    }
}
