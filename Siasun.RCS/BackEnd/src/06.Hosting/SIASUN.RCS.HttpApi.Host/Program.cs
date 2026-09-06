using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SIASUN.RCS.Infrastructure.Logging.Banner;

namespace SIASUN.RCS;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Async(c => c.File("Logs/logs.txt"))
            .WriteTo.Async(c => c.Console())
            .CreateBootstrapLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            // 根据 appsettings.json 中的 "Banner" 配置渲染并打印工业级控制台横幅
            RcsBannerRenderer.Print(builder.Configuration);

            Log.Information("Starting SIASUN.RCS.HttpApi.Host.");

            // 注册动态日志级别切换器 (单例)
            var logSwitchRegistry = new Infrastructure.Logging.DynamicLogSwitchRegistry();
            builder.Services.AddSingleton(logSwitchRegistry);

            builder.Host
                .AddAppSettingsSecretsJson()
                .UseAutofac()
                .UseSerilog((context, services, loggerConfiguration) =>
                {
                    loggerConfiguration.MinimumLevel.ControlledBy(logSwitchRegistry.GlobalSwitch);

                    foreach (var kvp in logSwitchRegistry.NamespaceSwitches)
                    {
                        loggerConfiguration.MinimumLevel.Override(kvp.Key, kvp.Value);
                    }

                    loggerConfiguration
                        .ReadFrom.Configuration(context.Configuration)
                        .ReadFrom.Services(services)
                        .WriteTo.Async(c => c.AbpStudio(services));
                });
            await builder.AddApplicationAsync<RCSHttpApiHostModule>();
            var app = builder.Build();
            await app.InitializeApplicationAsync();
            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            if (ex is HostAbortedException)
            {
                throw;
            }

            Log.Fatal(ex, "Host terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
