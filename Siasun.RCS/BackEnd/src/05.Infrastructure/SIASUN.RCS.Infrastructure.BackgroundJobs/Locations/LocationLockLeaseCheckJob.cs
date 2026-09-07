using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;
using SIASUN.RCS.Locations;
using SIASUN.RCS.Locations.Events;
using SIASUN.RCS.Monitor;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Uow;

namespace SIASUN.RCS.Infrastructure.BackgroundJobs.Locations
{
    /// <summary>
    /// 库位锁租约超期巡检与告警 Quartz 后台定时作业
    /// 积极监控车间掉电失联小车遗留的僵尸锁，触发自愈与全屏告警
    /// </summary>
    [DisallowConcurrentExecution]
    public class LocationLockLeaseCheckJob : IJob
    {
        private readonly IRepository<LocationLock, Guid> _lockRepository;
        private readonly IRepository<SystemEventLog, Guid> _systemEventLogRepository;
        private readonly ILocalEventBus _localEventBus;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly ILogger<LocationLockLeaseCheckJob> _logger;

        public LocationLockLeaseCheckJob(
            IRepository<LocationLock, Guid> lockRepository,
            IRepository<SystemEventLog, Guid> systemEventLogRepository,
            ILocalEventBus localEventBus,
            IUnitOfWorkManager unitOfWorkManager,
            ILogger<LocationLockLeaseCheckJob> logger)
        {
            _lockRepository = lockRepository;
            _systemEventLogRepository = systemEventLogRepository;
            _localEventBus = localEventBus;
            _unitOfWorkManager = unitOfWorkManager;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var nowUtc = DateTime.UtcNow;

            using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
            try
            {
                var query = await _lockRepository.GetQueryableAsync();
                var expiredLocks = query
                    .Where(l => l.LockType != LocationLockType.Maintenance && l.LeaseExpirationTime < nowUtc)
                    .ToList();

                if (expiredLocks.Count == 0)
                {
                    await uow.CompleteAsync(context.CancellationToken);
                    return;
                }

                _logger.LogWarning("库位锁巡检发现 {Count} 个已超期的僵尸锁，准备触发事件与系统告警", expiredLocks.Count);

                foreach (var lockItem in expiredLocks)
                {
                    var msg = $"库位 [{lockItem.LocationCode}] 锁租约已严重超时（持锁小车: {lockItem.VehicleCode}, 任务: {lockItem.TaskId}），小车可能掉电或异常卡死";
                    _logger.LogError(msg);

                    // 1. 记录系统关键时序事件
                    var sysEvent = new SystemEventLog(
                        Guid.NewGuid(),
                        "Location",
                        "Error",
                        msg,
                        $"LockType={lockItem.LockType}, TaskId={lockItem.TaskId}, Vehicle={lockItem.VehicleCode}",
                        nowUtc);

                    await _systemEventLogRepository.InsertAsync(sysEvent, autoSave: true, cancellationToken: context.CancellationToken);

                    // 2. 发布本地领域事件（联动 SignalR 推流与报警弹窗）
                    await _localEventBus.PublishAsync(new LocationLockExpiredEvent(
                        lockItem.LocationCode,
                        lockItem.TaskId,
                        lockItem.VehicleCode,
                        lockItem.LeaseExpirationTime,
                        nowUtc));
                }

                await uow.CompleteAsync(context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行库位锁租约超期巡检作业时发生异常");
            }
        }
    }
}
