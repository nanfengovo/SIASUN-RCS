using System;
using System.IO;
using System.Threading.Tasks;
using Shouldly;
using SIASUN.RCS.Infrastructure.AuditLog.Sqlite;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Tests.AuditLog.Sqlite
{
    public class AuditLogCleanupWorkerTests
    {
        [Fact]
        public void Job_Should_Be_Configured()
        {
            var worker = new AuditLogCleanupWorker();
            worker.JobDetail.Key.Name.ShouldBe(nameof(AuditLogCleanupWorker));
            worker.JobDetail.Key.Group.ShouldBe("MaintenanceGroup");
        }
    }
}
