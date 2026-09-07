using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Interfaces.OperationLogs;
using SIASUN.RCS.Locations;
using SIASUN.RCS.Locations.Dtos;
using SIASUN.RCS.Locations.Events;
using SIASUN.RCS.Logs.OperatorLog;
using SIASUN.RCS.Logs.OperatorLogs;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Guids;
using Xunit;

namespace SIASUN.RCS.Application.Tests.Locations
{
    /// <summary>
    /// 库位与地图点位映射应用服务单元测试
    /// 验证点位映射 CRUD 业务校验、重复检查、领域事件发布与责任审计日志无死角记录
    /// </summary>
    public class LocationMapAppServiceTests
    {
        private readonly IRepository<LocationMap, Guid> _repository;
        private readonly ILocalEventBus _localEventBus;
        private readonly IOperationLogRecorder _opRecorder;
        private readonly IGuidGenerator _guidGenerator;
        private readonly LocationMapAppService _appService;

        public LocationMapAppServiceTests()
        {
            _repository = Substitute.For<IRepository<LocationMap, Guid>>();
            _localEventBus = Substitute.For<ILocalEventBus>();
            _opRecorder = Substitute.For<IOperationLogRecorder>();
            _guidGenerator = Substitute.For<IGuidGenerator>();
            _guidGenerator.Create().Returns(Guid.NewGuid());

            _appService = new LocationMapAppService(
                _repository,
                _localEventBus,
                _opRecorder,
                _guidGenerator);
        }

        [Fact]
        public async Task CreateAsync_Should_Insert_PublishEvent_And_Record_OperationLog()
        {
            // Arrange
            var input = new CreateLocationMapDto
            {
                LocationCode = "WB-01-PORT",
                Name = "焊线机1号上料口",
                StationCode = "2010",
                PreDockStationCode = "2008",
                MapCode = "FAB_2F",
                Heading = 90.0,
                Area = "WireBond",
                Description = "半导体封测焊线区机台"
            };

            _repository.FindAsync(Arg.Any<Expression<Func<LocationMap, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<LocationMap?>(null));

            // Act
            var result = await _appService.CreateAsync(input);

            // Assert
            result.ShouldNotBeNull();
            result.LocationCode.ShouldBe(input.LocationCode);
            result.StationCode.ShouldBe(input.StationCode);

            await _repository.Received(1).InsertAsync(Arg.Any<LocationMap>(), autoSave: true);

            await _localEventBus.Received(1).PublishAsync(Arg.Is<LocationMapChangedEvent>(e =>
                e.LocationCode == input.LocationCode && e.ChangeType == "Created"));

            _opRecorder.Received(1).Record(Arg.Is<OperationLogContext>(ctx =>
                ctx.Module == "Location" &&
                ctx.Action == "CreateLocationMap" &&
                ctx.TargetType == "LocationMap" &&
                ctx.TargetId == input.LocationCode
            ), OperationLogStatus.Success);
        }

        [Fact]
        public async Task CreateAsync_When_Duplicate_LocationCode_Should_Throw_UserFriendlyException()
        {
            // Arrange
            var existing = new LocationMap(Guid.NewGuid(), "DUP-01", "已有库位", "1001");
            _repository.FindAsync(Arg.Any<Expression<Func<LocationMap, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<LocationMap?>(existing));

            var input = new CreateLocationMapDto
            {
                LocationCode = "DUP-01",
                Name = "重复尝试创建",
                StationCode = "1002"
            };

            // Act & Assert
            var ex = await Should.ThrowAsync<UserFriendlyException>(async () =>
            {
                await _appService.CreateAsync(input);
            });

            ex.Message.ShouldContain("已存在，不可重复添加");
        }

        [Fact]
        public async Task UpdateAsync_Should_UpdateEntity_PublishEvent_And_Record_OperationLog()
        {
            // Arrange
            var id = Guid.NewGuid();
            var entity = new LocationMap(id, "WB-02", "原名称", "2020");

            _repository.FindAsync(id, Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<LocationMap?>(entity));

            var input = new UpdateLocationMapDto
            {
                Name = "新名称",
                StationCode = "2021",
                PreDockStationCode = "2019",
                IsEnabled = true
            };

            // Act
            var result = await _appService.UpdateAsync(id, input);

            // Assert
            result.StationCode.ShouldBe("2021");
            await _repository.Received(1).UpdateAsync(entity, autoSave: true);

            await _localEventBus.Received(1).PublishAsync(Arg.Is<LocationMapChangedEvent>(e =>
                e.LocationCode == "WB-02" && e.ChangeType == "Updated"));

            _opRecorder.Received(1).Record(Arg.Is<OperationLogContext>(ctx =>
                ctx.Module == "Location" &&
                ctx.Action == "UpdateLocationMap" &&
                ctx.BeforeState == "2020" &&
                ctx.AfterState == "2021"
            ), OperationLogStatus.Success);
        }

        [Fact]
        public async Task DeleteAsync_Should_Delete_PublishEvent_And_Record_OperationLog()
        {
            // Arrange
            var id = Guid.NewGuid();
            var entity = new LocationMap(id, "DEL-LOC", "待删除库位", "8000");

            _repository.FindAsync(id, Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<LocationMap?>(entity));

            // Act
            await _appService.DeleteAsync(id);

            // Assert
            await _repository.Received(1).DeleteAsync(entity, autoSave: true);

            await _localEventBus.Received(1).PublishAsync(Arg.Is<LocationMapChangedEvent>(e =>
                e.LocationCode == "DEL-LOC" && e.ChangeType == "Deleted"));

            _opRecorder.Received(1).Record(Arg.Is<OperationLogContext>(ctx =>
                ctx.Module == "Location" &&
                ctx.Action == "DeleteLocationMap" &&
                ctx.TargetId == "DEL-LOC"
            ), OperationLogStatus.Success);
        }
    }
}
