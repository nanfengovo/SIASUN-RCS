using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using SIASUN.RCS.EntityFrameworkCore;
using SIASUN.RCS.Locations;
using Xunit;

namespace SIASUN.RCS.EntityFrameworkCore.Locations
{
    /// <summary>
    /// 基于真实 SQLite 内存数据库的物理空间与库位原子锁核心闭环测试
    /// 严格验证并发冲突排他、死锁有序排序、同任务幂等重入、僵尸锁租约自愈、人工维护互斥与强制解锁 6 大铁律
    /// </summary>
    [Collection(RCSTestConsts.CollectionDefinitionName)]
    public class EfCoreLocationLockerTests : RCSEntityFrameworkCoreTestBase
    {
        private readonly ILocationLocker _locationLocker;
        private readonly RCSDbContext _dbContext;

        public EfCoreLocationLockerTests()
        {
            _locationLocker = GetRequiredService<ILocationLocker>();
            _dbContext = GetRequiredService<RCSDbContext>();
        }

        /// <summary>
        /// 1. 并发物理防撞：小车 1 成功持锁后，小车 2 尝试锁定相同库位应立即被拒绝并指明冲突来源
        /// </summary>
        [Fact]
        public async Task Concurrent_Lock_Conflict_Should_Reject_Second_Vehicle()
        {
            // Arrange
            var locationCode = "LOC-CONFLICT-01";
            var task1Id = Guid.NewGuid();
            var task2Id = Guid.NewGuid();
            var vehicle1 = "AGV-01";
            var vehicle2 = "AGV-02";

            var req1 = new LocationLockRequest(locationCode, LocationLockType.Fetch, TimeSpan.FromMinutes(2));
            var req2 = new LocationLockRequest(locationCode, LocationLockType.Put, TimeSpan.FromMinutes(2));

            // Act 1: 车 1 加锁成功
            var res1 = await _locationLocker.TryAcquireLocksAsync(task1Id, vehicle1, new[] { req1 });

            // Act 2: 车 2 申请相同库位
            var res2 = await _locationLocker.TryAcquireLocksAsync(task2Id, vehicle2, new[] { req2 });

            // Assert
            res1.IsSuccess.ShouldBeTrue();
            res2.IsSuccess.ShouldBeFalse();
            res2.ConflictedLocation.ShouldBe(locationCode);
            res2.ConflictedTaskId.ShouldBe(task1Id);
            res2.ConflictedVehicleCode.ShouldBe(vehicle1);
        }

        /// <summary>
        /// 2. 死锁防范有序加锁：逆序申请库位时，内部必须依照严格 Ordinal 字典序排序加锁，破除死锁环路
        /// </summary>
        [Fact]
        public async Task Anti_Deadlock_Sorting_Should_Lock_In_Consistent_Order()
        {
            // Arrange: 乱序输入 LOC-02 与 LOC-01
            var taskId = Guid.NewGuid();
            var vehicleCode = "AGV-SORT-01";
            var requests = new List<LocationLockRequest>
            {
                new LocationLockRequest("LOC-SORT-02", LocationLockType.Put),
                new LocationLockRequest("LOC-SORT-01", LocationLockType.Fetch)
            };

            // Act
            var result = await _locationLocker.TryAcquireLocksAsync(taskId, vehicleCode, requests);

            // Assert
            result.IsSuccess.ShouldBeTrue();

            var dbLocks = await _dbContext.LocationLocks
                .Where(l => l.TaskId == taskId)
                .OrderBy(l => l.LocationCode)
                .ToListAsync();

            dbLocks.Count.ShouldBe(2);
            dbLocks[0].LocationCode.ShouldBe("LOC-SORT-01");
            dbLocks[1].LocationCode.ShouldBe("LOC-SORT-02");
        }

        /// <summary>
        /// 3. 同任务幂等重入：相同任务重复申请已有库位锁，应直接成功并自动续期，不产生主键冲突
        /// </summary>
        [Fact]
        public async Task Idempotent_Reentrancy_Should_Succeed_And_Extend_Lease()
        {
            // Arrange
            var locationCode = "LOC-REENTRANT-01";
            var taskId = Guid.NewGuid();
            var vehicleCode = "AGV-01";

            var reqInitial = new LocationLockRequest(locationCode, LocationLockType.Fetch, TimeSpan.FromMinutes(1));
            var reqExtended = new LocationLockRequest(locationCode, LocationLockType.Fetch, TimeSpan.FromMinutes(10));

            // Act 1: 首次加锁
            var res1 = await _locationLocker.TryAcquireLocksAsync(taskId, vehicleCode, new[] { reqInitial });
            var lockBefore = await _dbContext.LocationLocks.AsNoTracking().FirstAsync(l => l.LocationCode == locationCode);

            // Act 2: 相同任务再次加锁（幂等重入并延长租约）
            var res2 = await _locationLocker.TryAcquireLocksAsync(taskId, vehicleCode, new[] { reqExtended });
            var lockAfter = await _dbContext.LocationLocks.AsNoTracking().FirstAsync(l => l.LocationCode == locationCode);

            // Assert
            res1.IsSuccess.ShouldBeTrue();
            res2.IsSuccess.ShouldBeTrue();
            lockAfter.Id.ShouldBe(lockBefore.Id); // 依然是同一条记录，未产生重复插入
            lockAfter.LeaseExpirationTime.ShouldBeGreaterThan(lockBefore.LeaseExpirationTime);
        }

        /// <summary>
        /// 4. 租约超期自愈回收：面对异常掉电或失联车辆遗留的超期僵尸锁，新任务应能惰性自愈抢占
        /// </summary>
        [Fact]
        public async Task Lease_Self_Healing_Should_Reclaim_Expired_Zombie_Lock()
        {
            // Arrange: 手工植入一条已超期的僵尸锁（代表掉电遗留）
            var locationCode = "LOC-ZOMBIE-01";
            var oldTaskId = Guid.NewGuid();
            var oldVehicle = "AGV-DEAD";
            // 通过负时长租约直接构造已超期的僵尸锁（代表掉电遗留）
            var zombieLock = new LocationLock(
                Guid.NewGuid(),
                locationCode,
                LocationLockType.Fetch,
                oldTaskId,
                oldVehicle,
                TimeSpan.FromMinutes(-5));
            await _dbContext.LocationLocks.AddAsync(zombieLock);
            await _dbContext.SaveChangesAsync();

            var newTaskId = Guid.NewGuid();
            var newVehicle = "AGV-ALIVE";
            var newReq = new LocationLockRequest(locationCode, LocationLockType.Put, TimeSpan.FromMinutes(5));

            // Act: 活体新小车申请该库位
            var result = await _locationLocker.TryAcquireLocksAsync(newTaskId, newVehicle, new[] { newReq });

            // Assert: 自愈接管成功
            result.IsSuccess.ShouldBeTrue();

            var currentLock = await _dbContext.LocationLocks.AsNoTracking().FirstAsync(l => l.LocationCode == locationCode);
            currentLock.TaskId.ShouldBe(newTaskId);
            currentLock.VehicleCode.ShouldBe(newVehicle);
            currentLock.LockType.ShouldBe(LocationLockType.Put);
            currentLock.LeaseExpirationTime.ShouldBeGreaterThan(DateTime.UtcNow);
        }

        /// <summary>
        /// 5. 人工维护锁互斥与解除：调度员将库位置于维护模式后排斥所有作业，解除维护后恢复可用
        /// </summary>
        [Fact]
        public async Task Maintenance_Lock_And_Unlock_Should_Enforce_Exclusive_Maintenance()
        {
            // Arrange
            var locationCode = "LOC-MAINT-01";
            var taskId = Guid.NewGuid();
            var vehicleCode = "AGV-01";
            var req = new LocationLockRequest(locationCode, LocationLockType.Fetch);

            // Act 1: 调度员执行维护封锁
            var lockMaintResult = await _locationLocker.LockForMaintenanceAsync(locationCode, "机台顶针故障停机检修");
            lockMaintResult.ShouldBeTrue();

            // Act 2: 车辆任务尝试抢占该库位 -> 应被严格排斥
            var acquireResult = await _locationLocker.TryAcquireLocksAsync(taskId, vehicleCode, new[] { req });
            acquireResult.IsSuccess.ShouldBeFalse();
            acquireResult.ConflictedVehicleCode.ShouldBe(LocationLockConsts.MaintenanceVehicleCode);

            // Act 3: 调度员检修完毕，解除维护封锁
            var unlockMaintResult = await _locationLocker.UnlockMaintenanceAsync(locationCode);
            unlockMaintResult.ShouldBeTrue();

            // Act 4: 车辆任务再次申请 -> 成功入场
            var reAcquireResult = await _locationLocker.TryAcquireLocksAsync(taskId, vehicleCode, new[] { req });
            reAcquireResult.IsSuccess.ShouldBeTrue();
        }

        /// <summary>
        /// 6. 强制解锁：调度员人工强制解除作业锁，原有锁记录被安全清理，新任务立即就绪
        /// </summary>
        [Fact]
        public async Task Force_Unlock_Should_Release_Active_Lock_And_Allow_New_Acquisition()
        {
            // Arrange
            var locationCode = "LOC-FORCE-01";
            var task1Id = Guid.NewGuid();
            var task2Id = Guid.NewGuid();

            var req1 = new LocationLockRequest(locationCode, LocationLockType.Fetch);
            var req2 = new LocationLockRequest(locationCode, LocationLockType.Put);

            await _locationLocker.TryAcquireLocksAsync(task1Id, "AGV-01", new[] { req1 });
            (await _locationLocker.IsLockedAsync(locationCode)).ShouldBeTrue();

            // Act 1: 调度员人工强制解锁
            var affectedTaskId = await _locationLocker.ForceUnlockAsync(locationCode, "产线急停人工拔料，手动清空库位");
            affectedTaskId.ShouldBe(task1Id);

            // Assert: 库位已释放
            (await _locationLocker.IsLockedAsync(locationCode)).ShouldBeFalse();

            // Act 2: 任务 2 立即可以成功申请
            var res2 = await _locationLocker.TryAcquireLocksAsync(task2Id, "AGV-02", new[] { req2 });
            res2.IsSuccess.ShouldBeTrue();
        }
    }
}
