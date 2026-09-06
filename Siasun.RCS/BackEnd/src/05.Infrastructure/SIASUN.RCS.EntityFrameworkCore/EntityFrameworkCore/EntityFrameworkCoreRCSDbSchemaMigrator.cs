using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIASUN.RCS.Data;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.EntityFrameworkCore;

/// <summary>
/// 基于 EF Core 的数据库结构迁移器
/// </summary>
public class EntityFrameworkCoreRCSDbSchemaMigrator
    : IRCSDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 初始化数据库结构迁移器
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    public EntityFrameworkCoreRCSDbSchemaMigrator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 执行数据库结构迁移
    /// </summary>
    /// <returns>异步任务</returns>
    public async Task MigrateAsync()
    {
        /* We intentionally resolving the RCSDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<RCSDbContext>()
            .Database
            .MigrateAsync();
    }
}
