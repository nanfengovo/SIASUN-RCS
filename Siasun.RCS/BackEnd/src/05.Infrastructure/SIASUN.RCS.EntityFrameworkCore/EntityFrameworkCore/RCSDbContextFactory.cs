using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SIASUN.RCS.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class RCSDbContextFactory : IDesignTimeDbContextFactory<RCSDbContext>
{
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
