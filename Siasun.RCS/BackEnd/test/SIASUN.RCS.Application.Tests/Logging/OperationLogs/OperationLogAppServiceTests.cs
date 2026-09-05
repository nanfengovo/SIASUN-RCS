using System;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Logs.OperatorLog;
using SIASUN.RCS.Logs.OperatorLogs;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace SIASUN.RCS.Application.Tests.Logging.OperationLogs
{
    public class OperationLogAppServiceTests
    {
        private readonly IRepository<OperationLog, Guid> _repo;
        private readonly OperationLogAppService _appService;

        public OperationLogAppServiceTests()
        {
            _repo = Substitute.For<IRepository<OperationLog, Guid>>();
            _appService = new OperationLogAppService(_repo);
        }

        [Fact]
        public async Task GetAsync_Should_Return_Mapped_Dto()
        {
            var id = Guid.NewGuid();
            var log = new OperationLog(
                id: id,
                operatorType: OperatorType.User,
                userId: Guid.NewGuid(),
                userName: "Admin",
                clientIp: "192.168.1.1",
                correlationId: "trace-999",
                module: "TaskManagement",
                action: "ForceCancel",
                targetType: "Task",
                targetId: "T-8888",
                status: OperationLogStatus.Success,
                description: "Force cancelled by operator",
                errorMessage: string.Empty);

            _repo.GetAsync(id).Returns(Task.FromResult(log));

            var result = await _appService.GetAsync(id);

            result.ShouldNotBeNull();
            result.Id.ShouldBe(id);
            result.UserName.ShouldBe("Admin");
            result.Action.ShouldBe("ForceCancel");
            result.TargetId.ShouldBe("T-8888");
            result.Status.ShouldBe(OperationLogStatus.Success);
        }
    }
}
