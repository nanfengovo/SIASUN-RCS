using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;

namespace SIASUN.RCS.Locations
{
    /// <summary>
    /// 基于 EF Core 的物理空间与库位原子锁实现
    /// 严格遵循双数据库兼容（SQL Server / SQLite）与死锁破除铁律
    /// </summary>
    public class EfCoreLocationLocker : ILocationLocker, ITransientDependency
    {
        private readonly RCSDbContext _dbContext;
        private readonly IGuidGenerator _guidGenerator;
        private readonly ILogger<EfCoreLocationLocker> _logger;

        public EfCoreLocationLocker(
            RCSDbContext dbContext,
            IGuidGenerator guidGenerator,
            ILogger<EfCoreLocationLocker> logger)
        {
            _dbContext = dbContext;
            _guidGenerator = guidGenerator;
            _logger = logger;
        }

        public async Task<LocationLockResult> TryAcquireLocksAsync(
            Guid taskId,
            string vehicleCode,
            IEnumerable<LocationLockRequest> requests,
            CancellationToken cancellationToken = default)
        {
            var requestList = requests?.ToList() ?? new List<LocationLockRequest>();
            if (requestList.Count == 0)
            {
                return LocationLockResult.Success();
            }

            // 【核心防死锁铁律】：严格依照字典序升序排序，统一全局加锁顺序，破除环路等待条件！
            var sortedRequests = requestList
                .OrderBy(r => r.LocationCode, StringComparer.Ordinal)
                .ToList();

            var locationCodes = sortedRequests.Select(r => r.LocationCode).Distinct().ToList();
            var nowUtc = DateTime.UtcNow;

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var existingLocks = await _dbContext.LocationLocks
                    .Where(l => locationCodes.Contains(l.LocationCode))
                    .ToDictionaryAsync(l => l.LocationCode, cancellationToken);

                foreach (var req in sortedRequests)
                {
                    if (existingLocks.TryGetValue(req.LocationCode, out var currentLock))
                    {
                        // 1. 人工维护封锁排他检查：处于人工维护状态时，排斥所有作业加锁
                        if (currentLock.LockType == LocationLockType.Maintenance)
                        {
                            await transaction.RollbackAsync(cancellationToken);
                            _logger.LogWarning(
                                "任务 {TaskId} 尝试抢占库位 {LocationCode} 失败，该库位正处于调度员人工维护封锁中",
                                taskId, req.LocationCode);

                            return LocationLockResult.Conflict(
                                req.LocationCode,
                                Guid.Empty,
                                LocationLockConsts.MaintenanceVehicleCode,
                                $"库位处于人工维护封锁中: {currentLock.Reason}");
                        }

                        // 2. 可重入检查：同一个任务合法重入，顺带续期
                        if (currentLock.TaskId == taskId)
                        {
                            currentLock.ExtendLease(req.LeaseDuration);
                            continue;
                        }

                        // 3. 僵尸锁过期检查：若原锁已超期，视为孤儿锁进行自愈覆盖接管
                        if (currentLock.IsExpired(nowUtc))
                        {
                            _logger.LogWarning(
                                "检测到库位 {LocationCode} 持有者任务 {OldTaskId} 锁租约已超期，执行自愈抢占给新任务 {NewTaskId}",
                                req.LocationCode, currentLock.TaskId, taskId);

                            currentLock.Reclaim(taskId, vehicleCode, req.LockType, req.LeaseDuration);
                            continue;
                        }

                        // 4. 真实冲突：被其他有效活跃任务锁定
                        await transaction.RollbackAsync(cancellationToken);
                        _logger.LogInformation(
                            "任务 {TaskId} 尝试抢占库位 {LocationCode} 失败，已被任务 {HoldingTaskId} ({VehicleCode}) 锁定",
                            taskId, req.LocationCode, currentLock.TaskId, currentLock.VehicleCode);

                        return LocationLockResult.Conflict(
                            req.LocationCode,
                            currentLock.TaskId,
                            currentLock.VehicleCode,
                            $"库位已被任务 {currentLock.TaskId} 锁定");
                    }

                    // 5. 库位当前空闲，新增锁定记录
                    var newLock = new LocationLock(
                        _guidGenerator.Create(),
                        req.LocationCode,
                        req.LockType,
                        taskId,
                        vehicleCode,
                        req.LeaseDuration);

                    await _dbContext.LocationLocks.AddAsync(newLock, cancellationToken);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return LocationLockResult.Success();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "任务 {TaskId} 批量申请库位锁发生异常，事务已安全回滚", taskId);
                throw;
            }
        }

        public async Task ReleaseLockAsync(string locationCode, Guid taskId, CancellationToken cancellationToken = default)
        {
            var lockItem = await _dbContext.LocationLocks
                .FirstOrDefaultAsync(l => l.LocationCode == locationCode && l.TaskId == taskId, cancellationToken);

            if (lockItem != null)
            {
                _dbContext.LocationLocks.Remove(lockItem);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("任务 {TaskId} 提前释放单库位锁: {LocationCode}", taskId, locationCode);
            }
        }

        public async Task ReleaseAllByTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            var taskLocks = await _dbContext.LocationLocks
                .Where(l => l.TaskId == taskId)
                .ToListAsync(cancellationToken);

            if (taskLocks.Count > 0)
            {
                _dbContext.LocationLocks.RemoveRange(taskLocks);
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("任务 {TaskId} 结束，已全量释放所持有的 {Count} 个库位锁", taskId, taskLocks.Count);
            }
        }

        public async Task<bool> RenewLeasesAsync(Guid taskId, TimeSpan extensionDuration, CancellationToken cancellationToken = default)
        {
            var taskLocks = await _dbContext.LocationLocks
                .Where(l => l.TaskId == taskId)
                .ToListAsync(cancellationToken);

            if (taskLocks.Count == 0) return false;

            foreach (var l in taskLocks)
            {
                l.ExtendLease(extensionDuration);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> LockForMaintenanceAsync(string locationCode, string reason, CancellationToken cancellationToken = default)
        {
            var nowUtc = DateTime.UtcNow;
            var existingLock = await _dbContext.LocationLocks
                .FirstOrDefaultAsync(l => l.LocationCode == locationCode, cancellationToken);

            if (existingLock != null)
            {
                if (existingLock.LockType == LocationLockType.Maintenance)
                {
                    return true; // 已经是维护状态
                }

                if (!existingLock.IsExpired(nowUtc))
                {
                    // 存在未过期的作业任务锁，不能直接强加维护锁
                    return false;
                }

                // 孤儿锁过期自愈覆盖为维护锁
                existingLock.Reclaim(Guid.Empty, LocationLockConsts.MaintenanceVehicleCode, LocationLockType.Maintenance, TimeSpan.Zero, reason);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return true;
            }

            var maintenanceLock = LocationLock.CreateMaintenanceLock(_guidGenerator.Create(), locationCode, reason);
            await _dbContext.LocationLocks.AddAsync(maintenanceLock, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> UnlockMaintenanceAsync(string locationCode, CancellationToken cancellationToken = default)
        {
            var existingLock = await _dbContext.LocationLocks
                .FirstOrDefaultAsync(l => l.LocationCode == locationCode && l.LockType == LocationLockType.Maintenance, cancellationToken);

            if (existingLock == null)
            {
                return false;
            }

            _dbContext.LocationLocks.Remove(existingLock);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<Guid?> ForceUnlockAsync(string locationCode, string reason, CancellationToken cancellationToken = default)
        {
            var existingLock = await _dbContext.LocationLocks
                .FirstOrDefaultAsync(l => l.LocationCode == locationCode, cancellationToken);

            if (existingLock == null)
            {
                return null;
            }

            var previousTaskId = existingLock.TaskId != Guid.Empty ? existingLock.TaskId : (Guid?)null;
            _dbContext.LocationLocks.Remove(existingLock);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogWarning("调度员强制解除库位 [{LocationCode}] 的锁定 (原持锁任务: {PreviousTaskId}, 原因: {Reason})",
                locationCode, previousTaskId, reason);

            return previousTaskId;
        }

        public async Task<bool> IsLockedAsync(string locationCode, CancellationToken cancellationToken = default)
        {
            var nowUtc = DateTime.UtcNow;
            var lockItem = await _dbContext.LocationLocks
                .FirstOrDefaultAsync(l => l.LocationCode == locationCode, cancellationToken);

            if (lockItem == null) return false;
            return !lockItem.IsExpired(nowUtc);
        }
    }
}
