using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.EntityFrameworkCore.Auditing;
using Volo.Abp.Tracing;
using Xunit;

namespace SIASUN.RCS.EntityFrameworkCore.Auditing
{
    public class EntityAuditInterceptorTests
    {
        public class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class TestDbContext : DbContext
        {
            public DbSet<TestEntity> TestEntities { get; set; } = null!;

            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
            {
            }
        }

        [Fact]
        public async Task Should_Capture_Entity_Modification()
        {
            // Arrange
            var services = new ServiceCollection();
            
            var mockCorrelationProvider = Substitute.For<ICorrelationIdProvider>();
            mockCorrelationProvider.Get().Returns("test-trace-id");
            services.AddSingleton(mockCorrelationProvider);
            
            var channel = new EntityAuditLogChannel();
            services.AddSingleton(channel);
            
            var serviceProvider = services.BuildServiceProvider();
            var interceptor = new EntityAuditInterceptor(serviceProvider);

            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite("DataSource=:memory:")
                .AddInterceptors(interceptor)
                .Options;

            using var dbContext = new TestDbContext(options);
            await dbContext.Database.OpenConnectionAsync();
            await dbContext.Database.EnsureCreatedAsync();

            var entity = new TestEntity { Name = "OldName" };
            dbContext.TestEntities.Add(entity);
            await dbContext.SaveChangesAsync(); // 这一步应该被截获为 Added

            // 读掉 Added 的记录，清空通道状态
            while (channel.Reader.TryRead(out _)) { }

            // Act: 模拟修改操作
            entity.Name = "NewName";
            await dbContext.SaveChangesAsync(); // 这一步应该被截获为 Modified

            // Assert
            var success = channel.Reader.TryRead(out var logEntry);
            
            success.ShouldBeTrue();
            logEntry.ShouldNotBeNull();
            logEntry.EntityName.ShouldBe("TestEntity");
            logEntry.Action.ShouldBe("Modified");
            logEntry.TraceId.ShouldBe("test-trace-id");
            logEntry.PropertyChangesJson.ShouldContain("OldName");
            logEntry.PropertyChangesJson.ShouldContain("NewName");
        }
    }
}
