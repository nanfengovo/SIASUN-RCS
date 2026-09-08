using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace SIASUN.RCS.Infrastructure.Resilience
{
    /// <summary>
    /// 工业级通信底座模块（SocketsHttpHandler + Polly v8 弹性策略通用执行器）
    /// </summary>
    public class RCSInfrastructureResilienceModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IResilienceExecutor, ResiliencePipelineExecutor>();

            // 注册 SocketsHttpHandler 优化的 HttpClient 工厂
            context.Services.AddHttpClient("SiasunTm")
                .ConfigurePrimaryHttpMessageHandler(ResilientSocketsHttpHandlerFactory.CreateHandler)
                .SetHandlerLifetime(System.TimeSpan.FromMinutes(15));

            context.Services.AddHttpClient("StkcClient")
                .ConfigurePrimaryHttpMessageHandler(ResilientSocketsHttpHandlerFactory.CreateHandler)
                .SetHandlerLifetime(System.TimeSpan.FromMinutes(15));
        }
    }
}
