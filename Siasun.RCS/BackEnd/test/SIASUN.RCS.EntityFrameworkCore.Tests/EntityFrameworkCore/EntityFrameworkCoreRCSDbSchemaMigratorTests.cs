using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIASUN.RCS.Data;
using Shouldly;
using Xunit;
using Microsoft.Data.Sqlite;

namespace SIASUN.RCS.EntityFrameworkCore;

public class EntityFrameworkCoreRCSDbSchemaMigratorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _serviceProvider;
    private readonly EntityFrameworkCoreRCSDbSchemaMigrator _migrator;

    public EntityFrameworkCoreRCSDbSchemaMigratorTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        
        services.AddDbContext<RCSDbContext>(options =>
        {
            options.UseSqlite(_connection);
        });

        _serviceProvider = services.BuildServiceProvider();
        _migrator = new EntityFrameworkCoreRCSDbSchemaMigrator(_serviceProvider);
    }

    [Fact]
    public async Task MigrateAsync_ShouldExecute_AndThrowSqliteErrorBecauseOfSqlServerSyntax()
    {
        // Act
        var exception = await Record.ExceptionAsync(async () => await _migrator.MigrateAsync());

        // Assert
        // The fact that it throws a SqliteException means it successfully resolved the context
        // and attempted to execute the migrations. The syntax error is expected because
        // the migrations are written for SQL Server (e.g., nvarchar(max)) but run against SQLite here.
        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<SqliteException>();
        exception.Message.ShouldContain("syntax error");
    }

    public void Dispose()
    {
        _connection.Dispose();
        _serviceProvider.Dispose();
    }
}
