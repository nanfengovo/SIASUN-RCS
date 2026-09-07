using System;
using System.Threading.Tasks;
using Shouldly;
using SIASUN.RCS.EntityFrameworkCore;
using SIASUN.RCS.Locations;
using SIASUN.RCS.Locations.Events;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace SIASUN.RCS.EntityFrameworkCore.Locations
{
    /// <summary>
    /// 库位与 AGV 地图点位高速缓存解析器集成测试
    /// 验证内存字典亚毫秒级无锁寻址、安全禁用拦截与领域事件驱动热重载
    /// </summary>
    [Collection(RCSTestConsts.CollectionDefinitionName)]
    public class MemoryCachedLocationMapResolverTests : RCSEntityFrameworkCoreTestBase
    {
        private readonly ILocationMapResolver _resolver;
        private readonly IRepository<LocationMap, Guid> _repository;
        private readonly ILocalEventBus _localEventBus;

        public MemoryCachedLocationMapResolverTests()
        {
            _resolver = GetRequiredService<ILocationMapResolver>();
            _repository = GetRequiredService<IRepository<LocationMap, Guid>>();
            _localEventBus = GetRequiredService<ILocalEventBus>();
        }

        [Fact]
        public async Task ResolveStationCodeAsync_Should_Return_Mapped_Station_And_Details()
        {
            // Arrange
            var locationCode = "EQP-MOLD-01";
            var stationCode = "3050";
            var preDock = "3048";
            var entity = new LocationMap(
                Guid.NewGuid(),
                locationCode,
                "1号成型机台",
                stationCode,
                preDockStationCode: preDock,
                heading: 270.0,
                area: "Molding");

            await _repository.InsertAsync(entity, autoSave: true);
            await _resolver.RefreshCacheAsync();

            // Act 1: 解析站点
            var resolvedStation = await _resolver.ResolveStationCodeAsync(locationCode);

            // Assert 1
            resolvedStation.ShouldBe(stationCode);

            // Act 2: 获取完整点位详情
            var mappingInfo = await _resolver.GetMappingAsync(locationCode);

            // Assert 2
            mappingInfo.ShouldNotBeNull();
            mappingInfo.LocationCode.ShouldBe(locationCode);
            mappingInfo.StationCode.ShouldBe(stationCode);
            mappingInfo.PreDockStationCode.ShouldBe(preDock);
            mappingInfo.Heading.ShouldBe(270.0);
            mappingInfo.Area.ShouldBe("Molding");
            mappingInfo.IsEnabled.ShouldBeTrue();
        }

        [Fact]
        public async Task Disabled_Location_Should_Resolve_To_Null()
        {
            // Arrange: 处于禁用维护状态的点位映射
            var locationCode = "LOC-DISABLED-01";
            var entity = new LocationMap(
                Guid.NewGuid(),
                locationCode,
                "停运测试机台",
                "9999",
                isEnabled: false);

            await _repository.InsertAsync(entity, autoSave: true);
            await _resolver.RefreshCacheAsync();

            // Act & Assert: 禁用点位在调度解析时必须返回 null，防止小车误闯停用区域
            var resolvedStation = await _resolver.ResolveStationCodeAsync(locationCode);
            resolvedStation.ShouldBeNull();

            var mapping = await _resolver.GetMappingAsync(locationCode);
            mapping.ShouldBeNull();
        }

        [Fact]
        public async Task NonExistent_Location_Should_Return_Null()
        {
            var resolvedStation = await _resolver.ResolveStationCodeAsync("NON-EXISTENT-LOC");
            resolvedStation.ShouldBeNull();
        }

        [Fact]
        public async Task LocationMapChangedEvent_Should_Trigger_HotReload_Without_Restart()
        {
            // Arrange
            var locationCode = "HOT-RELOAD-LOC";
            var initialEntity = new LocationMap(Guid.NewGuid(), locationCode, "热重载点位", "5001");
            await _repository.InsertAsync(initialEntity, autoSave: true);
            await _resolver.RefreshCacheAsync();

            (await _resolver.ResolveStationCodeAsync(locationCode)).ShouldBe("5001");

            // Act: 现场实施人员在不重启服务的情况下将点位修改为 5002
            initialEntity.UpdateDetails("热重载点位", "5002", null, null, null, null, null);
            await _repository.UpdateAsync(initialEntity, autoSave: true);

            // 发布领域事件模拟应用服务触发的变更通知
            await _localEventBus.PublishAsync(new LocationMapChangedEvent(locationCode, "Updated"));

            // Assert: 解析器在无需系统重启的情况下实时感知最新点位！
            var updatedStation = await _resolver.ResolveStationCodeAsync(locationCode);
            updatedStation.ShouldBe("5002");
        }
    }
}
