using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SIASUN.RCS.Data;
using Serilog;
using Volo.Abp;
using Volo.Abp.Data;

namespace SIASUN.RCS.DbMigrator;

/// <summary>
/// 数据库迁移独立宿主后台服务
/// </summary>
public class DbMigratorHostedService : IHostedService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// 初始化迁移服务
    /// </summary>
    /// <param name="hostApplicationLifetime">应用程序生命周期管理器</param>
    /// <param name="configuration">配置提供者</param>
    public DbMigratorHostedService(IHostApplicationLifetime hostApplicationLifetime, IConfiguration configuration)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _configuration = configuration;
    }

    /// <summary>
    /// 启动迁移作业
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步任务</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using (var application = await AbpApplicationFactory.CreateAsync<RCSDbMigratorModule>(options =>
        {
           options.Services.ReplaceConfiguration(_configuration);
           options.UseAutofac();
           options.Services.AddLogging(c => c.AddSerilog());
           options.AddDataMigrationEnvironment();
        }))
        {
            await application.InitializeAsync();

            await application
                .ServiceProvider
                .GetRequiredService<RCSDbMigrationService>()
                .MigrateAsync();

            await application.ShutdownAsync();

            _hostApplicationLifetime.StopApplication();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
