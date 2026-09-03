using System;
using Shouldly;
using SIASUN.RCS.Monitor;
using Xunit;

namespace SIASUN.RCS.Domain.Tests.Monitor
{
    public class SystemEventLogTests
    {
        [Fact]
        public void Constructor_Should_Set_Properties_Correctly()
        {
            var id = Guid.NewGuid();
            var log = new SystemEventLog(
                id: id,
                eventCategory: "DiskSelfHeal",
                level: "Warning",
                message: "Disk space running low",
                actionDetails: "Deleted 3 expired log files"
            );

            log.Id.ShouldBe(id);
            log.EventCategory.ShouldBe("DiskSelfHeal");
            log.Level.ShouldBe("Warning");
            log.Message.ShouldBe("Disk space running low");
            log.ActionDetails.ShouldBe("Deleted 3 expired log files");
        }
    }
}
