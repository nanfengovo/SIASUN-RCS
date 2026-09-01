using Shouldly;
using SIASUN.RCS.Infrastructure.AuditLog.Sqlite;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Tests.AuditLog.Sqlite
{
    /// <summary>
    /// 测试 AuditLogCleanupJob（已由 QuartzBackgroundWorkerBase 重构为纯 IJob）
    /// </summary>
    public class AuditLogCleanupJobTests
    {
        [Fact]
        public void AuditLogCleanupJob_Should_Implement_IJob()
        {
            typeof(AuditLogCleanupJob)
                .GetInterface(nameof(Quartz.IJob))
                .ShouldNotBeNull();
        }

        [Fact]
        public void AuditLogCleanupJob_Should_Have_DisallowConcurrentExecution()
        {
            typeof(AuditLogCleanupJob)
                .GetCustomAttributes(typeof(Quartz.DisallowConcurrentExecutionAttribute), inherit: false)
                .Length
                .ShouldBe(1);
        }
    }
}
