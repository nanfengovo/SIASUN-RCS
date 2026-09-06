using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SIASUN.RCS.EntityFrameworkCore;

/// <summary>
/// EF Core 设计时 DbContext 实例工厂（用于 Add-Migration 与 Update-Database 命令）
/// </summary>
public class RCSDbContextFactory : IDesignTimeDbContextFactory<RCSDbContext>
{
    /// <summary>
    /// 创建设计时 DbContext 实例
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <returns>RCS 数据库上下文实例</returns>
    public RCSDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();

        RCSEfCoreEntityExtensionMappings.Configure();

        var builder = new DbContextOptionsBuilder<RCSDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));

        return new RCSDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../SIASUN.RCS.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables();

        return builder.Build();
    }
}
