using System;
using System.Collections.Generic;
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
            
            // Mock IEntityAuditLogChannel interface
            var mockChannel = Substitute.For<IEntityAuditLogChannel>();
            services.AddSingleton(mockChannel);
            
            var mockEvaluator = Substitute.For<IEntityAuditRuleEvaluator>();
            mockEvaluator.Evaluate(Arg.Any<string>(), Arg.Any<string>()).Returns(EntityAuditMode.Full);
            services.AddSingleton(mockEvaluator);
            
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

            // Act: 模拟修改操作
            entity.Name = "NewName";
            await dbContext.SaveChangesAsync(); // 这一步应该被截获为 Modified

            // Assert
            mockChannel.Received().TryWrite(Arg.Is<EntityAuditLogMessage>(x => x.Action == "Modified" && x.EntityName == "TestEntity"));
        }
    }
}
