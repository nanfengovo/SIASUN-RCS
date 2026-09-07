using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using SIASUN.RCS.EntityFrameworkCore;
using SIASUN.RCS.Locations;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace SIASUN.RCS.EntityFrameworkCore.Locations
{
    /// <summary>
    /// 业务库位与地图点位映射 EF Core 持久化与数据库集成测试
    /// 验证映射实体在 SQLite 内存数据库中的 CRUD 持久化、字段限长与全局唯一索引约束
    /// </summary>
    [Collection(RCSTestConsts.CollectionDefinitionName)]
    public class EfCoreLocationMapTests : RCSEntityFrameworkCoreTestBase
    {
        private readonly IRepository<LocationMap, Guid> _repository;
        private readonly RCSDbContext _dbContext;

        public EfCoreLocationMapTests()
        {
            _repository = GetRequiredService<IRepository<LocationMap, Guid>>();
            _dbContext = GetRequiredService<RCSDbContext>();
        }

        [Fact]
        public async Task LocationMap_CRUD_Persistence_Should_Work_Correctly()
        {
            // Arrange
            var id = Guid.NewGuid();
            var locationCode = "STK-IN-01";
            var entity = new LocationMap(
                id,
                locationCode,
                "立库入料接驳台",
                "1024",
                preDockStationCode: "1020",
                mapCode: "MAP_FAB_1F",
                heading: 90.0,
                area: "CleanRoom",
                description: "立库 1 号入口取料停靠点");

            // Act 1: 插入记录
            await _repository.InsertAsync(entity, autoSave: true);

            // Assert 1: 读取并验证
            var saved = await _dbContext.LocationMaps.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            saved.ShouldNotBeNull();
            saved.LocationCode.ShouldBe(locationCode);
            saved.StationCode.ShouldBe("1024");
            saved.PreDockStationCode.ShouldBe("1020");
            saved.MapCode.ShouldBe("MAP_FAB_1F");
            saved.Heading.ShouldBe(90.0);
            saved.Area.ShouldBe("CleanRoom");
            saved.IsEnabled.ShouldBeTrue();

            // Act 2: 更新点位信息（现场 CAD 调整点位号）
            entity.UpdateDetails(
                "立库入料接驳台-微调",
                "1025",
                "1021",
                "MAP_FAB_1F",
                180.0,
                "CleanRoom",
                "现场标定调整");
            await _repository.UpdateAsync(entity, autoSave: true);

            // Assert 2: 验证更新落库
            var updated = await _dbContext.LocationMaps.AsNoTracking().FirstAsync(x => x.Id == id);
            updated.StationCode.ShouldBe("1025");
            updated.Heading.ShouldBe(180.0);

            // Act 3: 删除记录
            await _repository.DeleteAsync(entity, autoSave: true);

            // Assert 3: 验证删除
            var deleted = await _dbContext.LocationMaps.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            deleted.ShouldBeNull();
        }

        [Fact]
        public async Task LocationCode_Unique_Index_Should_Prevent_Duplicates()
        {
            // Arrange
            var locationCode = "DUP-LOC-01";
            var map1 = new LocationMap(Guid.NewGuid(), locationCode, "点位1", "1001");
            var map2 = new LocationMap(Guid.NewGuid(), locationCode, "点位2", "1002");

            await _repository.InsertAsync(map1, autoSave: true);

            // Act & Assert: 尝试插入相同 LocationCode 的记录应引发唯一索引异常
            await Should.ThrowAsync<DbUpdateException>(async () =>
            {
                await _repository.InsertAsync(map2, autoSave: true);
            });
        }
    }
}
