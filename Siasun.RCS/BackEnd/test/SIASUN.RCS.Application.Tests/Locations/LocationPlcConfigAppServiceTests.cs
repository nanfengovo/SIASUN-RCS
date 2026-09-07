using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Interfaces.OperationLogs;
using SIASUN.RCS.Locations;
using SIASUN.RCS.Locations.Dtos;
using SIASUN.RCS.Logs.OperatorLog;
using SIASUN.RCS.Logs.OperatorLogs;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Xunit;

namespace SIASUN.RCS.Application.Tests.Locations
{
    /// <summary>
    /// 库位 PLC 硬件联锁配置应用服务单元测试
    /// 验证点表配置 CRUD、重复校验与操作审计记录
    /// </summary>
    public class LocationPlcConfigAppServiceTests
    {
        private readonly IRepository<LocationPlcConfig, Guid> _repository;
        private readonly IOperationLogRecorder _opRecorder;
        private readonly IGuidGenerator _guidGenerator;
        private readonly LocationPlcConfigAppService _appService;

        public LocationPlcConfigAppServiceTests()
        {
            _repository = Substitute.For<IRepository<LocationPlcConfig, Guid>>();
            _opRecorder = Substitute.For<IOperationLogRecorder>();
            _guidGenerator = Substitute.For<IGuidGenerator>();
            _guidGenerator.Create().Returns(Guid.NewGuid());

            _appService = new LocationPlcConfigAppService(
                _repository,
                _opRecorder,
                _guidGenerator);
        }

        [Fact]
        public async Task CreateAsync_Should_Insert_And_Record_OperationLog()
        {
            // Arrange
            var input = new CreateLocationPlcConfigDto
            {
                LocationCode = "EQP-01",
                GatewayId = "PLC-LINE-01",
                MaterialPresenceTag = "DB100.DBX0.0",
                InterlockReadyTag = "DB100.DBX0.1",
                TimeoutSeconds = 45,
                Description = "1号机台光电与放行信号"
            };

            _repository.FindAsync(Arg.Any<Expression<Func<LocationPlcConfig, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<LocationPlcConfig?>(null));

            // Act
            var result = await _appService.CreateAsync(input);

            // Assert
            result.LocationCode.ShouldBe(input.LocationCode);
            result.GatewayId.ShouldBe(input.GatewayId);
            result.MaterialPresenceTag.ShouldBe(input.MaterialPresenceTag);
            result.TimeoutSeconds.ShouldBe(45);

            await _repository.Received(1).InsertAsync(Arg.Any<LocationPlcConfig>(), autoSave: true);

            _opRecorder.Received(1).Record(Arg.Is<OperationLogContext>(ctx =>
                ctx.Module == "Location" &&
                ctx.Action == "CreateLocationPlcConfig" &&
                ctx.TargetId == input.LocationCode
            ), OperationLogStatus.Success);
        }

        [Fact]
        public async Task CreateAsync_When_Duplicate_Should_Throw_UserFriendlyException()
        {
            // Arrange
            var existing = new LocationPlcConfig(Guid.NewGuid(), "DUP-PLC", true);
            _repository.FindAsync(Arg.Any<Expression<Func<LocationPlcConfig, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<LocationPlcConfig?>(existing));

            var input = new CreateLocationPlcConfigDto
            {
                LocationCode = "DUP-PLC"
            };

            // Act & Assert
            var ex = await Should.ThrowAsync<UserFriendlyException>(async () =>
            {
                await _appService.CreateAsync(input);
            });

            ex.Message.ShouldContain("已存在，不可重复添加");
        }

        [Fact]
        public async Task UpdateAsync_Should_Mutate_And_Record_OperationLog()
        {
            // Arrange
            var id = Guid.NewGuid();
            var entity = new LocationPlcConfig(id, "LOC-01", true, "OLD-GW", "DB1.0", "DB1.1", 30);

            _repository.FindAsync(id, Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<LocationPlcConfig?>(entity));

            var input = new UpdateLocationPlcConfigDto
            {
                IsEnabled = false,
                GatewayId = "NEW-GW",
                MaterialPresenceTag = "DB2.0",
                InterlockReadyTag = "DB2.1",
                TimeoutSeconds = 60,
                Description = "修改为备用网关"
            };

            // Act
            var result = await _appService.UpdateAsync(id, input);

            // Assert
            result.IsEnabled.ShouldBeFalse();
            result.GatewayId.ShouldBe("NEW-GW");
            result.TimeoutSeconds.ShouldBe(60);

            await _repository.Received(1).UpdateAsync(entity, autoSave: true);

            _opRecorder.Received(1).Record(Arg.Is<OperationLogContext>(ctx =>
                ctx.Module == "Location" &&
                ctx.Action == "UpdateLocationPlcConfig" &&
                ctx.TargetId == "LOC-01"
            ), OperationLogStatus.Success);
        }

        [Fact]
        public async Task DeleteAsync_Should_Delete_And_Record_OperationLog()
        {
            // Arrange
            var id = Guid.NewGuid();
            var entity = new LocationPlcConfig(id, "DEL-LOC", true);

            _repository.FindAsync(id, Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<LocationPlcConfig?>(entity));

            // Act
            await _appService.DeleteAsync(id);

            // Assert
            await _repository.Received(1).DeleteAsync(entity, autoSave: true);

            _opRecorder.Received(1).Record(Arg.Is<OperationLogContext>(ctx =>
                ctx.Module == "Location" &&
                ctx.Action == "DeleteLocationPlcConfig" &&
                ctx.TargetId == "DEL-LOC"
            ), OperationLogStatus.Success);
        }
    }
}
