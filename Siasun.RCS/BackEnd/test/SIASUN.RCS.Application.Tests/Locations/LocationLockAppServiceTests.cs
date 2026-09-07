using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Commands;
using SIASUN.RCS.Interfaces.OperationLogs;
using SIASUN.RCS.Locations;
using SIASUN.RCS.Locations.Commands;
using SIASUN.RCS.Locations.Dtos;
using SIASUN.RCS.Logs.OperatorLog;
using SIASUN.RCS.Logs.OperatorLogs;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace SIASUN.RCS.Application.Tests.Locations
{
    /// <summary>
    /// 库位锁应用服务与 CQRS 操作日志审计集成单元测试
    /// 验证调度员人工干预命令（强制解锁、维护封锁、解除维护）能够自动穿透 MediatR 审计管道，
    /// 形成 100% 不可抵赖的操作责任审计记录（记录 BeforeState、AfterState、操作原因与耗时）
    /// </summary>
    public class LocationLockAppServiceTests
    {
        private readonly IOperationLogRecorder _opRecorder;
        private readonly ILocationLocker _locationLocker;
        private readonly IRepository<LocationLock, Guid> _lockRepository;
        private readonly LocationLockAppService _appService;

        public LocationLockAppServiceTests()
        {
            _opRecorder = Substitute.For<IOperationLogRecorder>();
            _locationLocker = Substitute.For<ILocationLocker>();
            _lockRepository = Substitute.For<IRepository<LocationLock, Guid>>();

            var services = new ServiceCollection();
            services.AddSingleton(_opRecorder);
            services.AddSingleton(_locationLocker);
            services.AddSingleton(_lockRepository);
            services.AddLogging();
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(RCSApplicationModule).Assembly);
                cfg.AddOpenBehavior(typeof(CommandAuditPipelineBehavior<,>));
            });

            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<MediatR.IMediator>();
            _appService = new LocationLockAppService(mediator, _lockRepository);
        }

        [Fact]
        public async Task ForceUnlockAsync_Should_InvokeLocker_And_Record_Success_Audit()
        {
            // Arrange
            var locationCode = "LOC-FORCE-001";
            var reason = "机台急停人工清料";
            var activeTaskId = Guid.NewGuid();

            _locationLocker.ForceUnlockAsync(locationCode, reason, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Guid?>(activeTaskId));

            var input = new ForceUnlockLocationInput
            {
                LocationCode = locationCode,
                Reason = reason
            };

            // Act
            var result = await _appService.ForceUnlockAsync(input);

            // Assert
            result.Success.ShouldBeTrue();
            result.LocationCode.ShouldBe(locationCode);
            result.AffectedTaskId.ShouldBe(activeTaskId);
            result.BeforeState.ShouldBe("Locked");
            result.AfterState.ShouldBe("Unlocked");

            await _locationLocker.Received(1).ForceUnlockAsync(locationCode, reason, Arg.Any<CancellationToken>());

            _opRecorder.Received(1).Record(Arg.Is<OperationLogContext>(ctx =>
                ctx.Module == "Location" &&
                ctx.Action == "ForceUnlock" &&
                ctx.TargetType == "Location" &&
                ctx.TargetId == locationCode &&
                ctx.BeforeState == "Locked" &&
                ctx.AfterState == "Unlocked" &&
                ctx.Reason == reason
            ), OperationLogStatus.Success);
        }

        [Fact]
        public async Task LockForMaintenanceAsync_WhenSuccess_Should_Record_Maintenance_Audit()
        {
            // Arrange
            var locationCode = "LOC-MAINT-001";
            var reason = "周检传感器标定维护";

            _locationLocker.LockForMaintenanceAsync(locationCode, reason, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            var input = new LockLocationForMaintenanceInput
            {
                LocationCode = locationCode,
                Reason = reason
            };

            // Act
            var result = await _appService.LockForMaintenanceAsync(input);

            // Assert
            result.Success.ShouldBeTrue();
            result.LocationCode.ShouldBe(locationCode);
            result.BeforeState.ShouldBe("Unlocked");
            result.AfterState.ShouldBe("Maintenance");

            _opRecorder.Received(1).Record(Arg.Is<OperationLogContext>(ctx =>
                ctx.Module == "Location" &&
                ctx.Action == "LockForMaintenance" &&
                ctx.TargetType == "Location" &&
                ctx.TargetId == locationCode &&
                ctx.BeforeState == "Unlocked" &&
                ctx.AfterState == "Maintenance" &&
                ctx.Reason == reason
            ), OperationLogStatus.Success);
        }

        [Fact]
        public async Task UnlockMaintenanceAsync_WhenSuccess_Should_Record_Unlock_Audit()
        {
            // Arrange
            var locationCode = "LOC-MAINT-001";
            var reason = "检修完毕恢复生产";

            _locationLocker.UnlockMaintenanceAsync(locationCode, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            var input = new UnlockLocationMaintenanceInput
            {
                LocationCode = locationCode,
                Reason = reason
            };

            // Act
            var result = await _appService.UnlockMaintenanceAsync(input);

            // Assert
            result.Success.ShouldBeTrue();
            result.LocationCode.ShouldBe(locationCode);
            result.BeforeState.ShouldBe("Maintenance");
            result.AfterState.ShouldBe("Unlocked");

            _opRecorder.Received(1).Record(Arg.Is<OperationLogContext>(ctx =>
                ctx.Module == "Location" &&
                ctx.Action == "UnlockMaintenance" &&
                ctx.TargetType == "Location" &&
                ctx.TargetId == locationCode &&
                ctx.BeforeState == "Maintenance" &&
                ctx.AfterState == "Unlocked" &&
                ctx.Reason == reason
            ), OperationLogStatus.Success);
        }

        [Fact]
        public async Task GetActiveLocksAsync_Should_Return_Mapped_Dtos()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var list = new List<LocationLock>
            {
                new LocationLock(Guid.NewGuid(), "LOC-01", LocationLockType.Fetch, taskId, "AGV-01", TimeSpan.FromMinutes(5)),
                LocationLock.CreateMaintenanceLock(Guid.NewGuid(), "LOC-02", "定期保养")
            };

            _lockRepository.GetListAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(list));

            // Act
            var dtos = await _appService.GetActiveLocksAsync();

            // Assert
            dtos.Count.ShouldBe(2);
            dtos[0].LocationCode.ShouldBe("LOC-01");
            dtos[0].LockType.ShouldBe(LocationLockType.Fetch);
            dtos[0].VehicleCode.ShouldBe("AGV-01");
            dtos[0].IsMaintenance.ShouldBeFalse();

            dtos[1].LocationCode.ShouldBe("LOC-02");
            dtos[1].LockType.ShouldBe(LocationLockType.Maintenance);
            dtos[1].IsMaintenance.ShouldBeTrue();
        }
    }
}
