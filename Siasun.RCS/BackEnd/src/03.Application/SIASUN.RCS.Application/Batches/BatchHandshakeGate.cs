using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace SIASUN.RCS.Batches
{
    /// <summary>
    /// 多车同批次门禁过滤器与互锁协同步道网关实现
    /// 遵循《AGENTS.md》铁律 6：支持一单分拆多子任务、汇聚同步、载具绑定（Carrier）与安全互锁编排
    /// 提供受限区域（立库口、洁净室风淋门、窄通道）基于批次的并发流控与汇聚同步检查
    /// </summary>
    public class BatchHandshakeGate : IBatchHandshakeGate, ISingletonDependency
    {
        private readonly IRepository<AgvTask, Guid> _taskRepository;
        private readonly ILogger<BatchHandshakeGate> _logger;

        /// <summary>
        /// 区域通行占用注册表: Key 为 "BatchCode:ZoneOrStation", Value 为当前处于该区域的车辆代码集合
        /// </summary>
        private readonly ConcurrentDictionary<string, HashSet<string>> _activeZoneOccupancies = new();
        private readonly object _gateLock = new();

        /// <summary>
        /// 构造函数注入任务仓储与日志
        /// </summary>
        /// <param name="taskRepository">AGV 任务仓储</param>
        /// <param name="logger">日志记录器</param>
        public BatchHandshakeGate(
            IRepository<AgvTask, Guid> taskRepository,
            ILogger<BatchHandshakeGate> logger)
        {
            _taskRepository = taskRepository;
            _logger = logger;
        }

        /// <inheritdoc />
        public Task<bool> CanVehicleEnterZoneAsync(
            string batchCode,
            string vehicleCode,
            string zoneOrStation,
            int maxConcurrentVehicles = 1,
            CancellationToken cancellationToken = default)
        {
            Check.NotNullOrWhiteSpace(batchCode, nameof(batchCode));
            Check.NotNullOrWhiteSpace(vehicleCode, nameof(vehicleCode));
            Check.NotNullOrWhiteSpace(zoneOrStation, nameof(zoneOrStation));

            var key = BuildGateKey(batchCode, zoneOrStation);

            lock (_gateLock)
            {
                if (!_activeZoneOccupancies.TryGetValue(key, out var vehicles))
                {
                    return Task.FromResult(true);
                }

                // 若该车已经在区域中，允许继续通行
                if (vehicles.Contains(vehicleCode))
                {
                    return Task.FromResult(true);
                }

                // 检查是否未超出最大允许并发数
                return Task.FromResult(vehicles.Count < maxConcurrentVehicles);
            }
        }

        /// <inheritdoc />
        public Task<bool> TryAcquireEntryAsync(
            string batchCode,
            string vehicleCode,
            string zoneOrStation,
            int maxConcurrentVehicles = 1,
            CancellationToken cancellationToken = default)
        {
            Check.NotNullOrWhiteSpace(batchCode, nameof(batchCode));
            Check.NotNullOrWhiteSpace(vehicleCode, nameof(vehicleCode));
            Check.NotNullOrWhiteSpace(zoneOrStation, nameof(zoneOrStation));

            var key = BuildGateKey(batchCode, zoneOrStation);

            lock (_gateLock)
            {
                var vehicles = _activeZoneOccupancies.GetOrAdd(key, _ => new HashSet<string>());

                if (vehicles.Contains(vehicleCode))
                {
                    _logger.LogDebug("车辆 [{Vehicle}] 已处于批次 [{Batch}] 区域 [{Zone}] 许可列表中，复用许可",
                        vehicleCode, batchCode, zoneOrStation);
                    return Task.FromResult(true);
                }

                if (vehicles.Count >= maxConcurrentVehicles)
                {
                    _logger.LogWarning("批次 [{Batch}] 区域 [{Zone}] 当前占用数已满 ({Current}/{Max})，拒绝车辆 [{Vehicle}] 进入",
                        batchCode, zoneOrStation, vehicles.Count, maxConcurrentVehicles, vehicleCode);
                    return Task.FromResult(false);
                }

                vehicles.Add(vehicleCode);
                _logger.LogInformation("批次 [{Batch}] 区域 [{Zone}] 成功授予车辆 [{Vehicle}] 通行许可 (当前占用 {Count}/{Max})",
                    batchCode, zoneOrStation, vehicleCode, vehicles.Count, maxConcurrentVehicles);
                return Task.FromResult(true);
            }
        }

        /// <inheritdoc />
        public Task ReleaseExitAsync(
            string batchCode,
            string vehicleCode,
            string zoneOrStation,
            CancellationToken cancellationToken = default)
        {
            Check.NotNullOrWhiteSpace(batchCode, nameof(batchCode));
            Check.NotNullOrWhiteSpace(vehicleCode, nameof(vehicleCode));
            Check.NotNullOrWhiteSpace(zoneOrStation, nameof(zoneOrStation));

            var key = BuildGateKey(batchCode, zoneOrStation);

            lock (_gateLock)
            {
                if (_activeZoneOccupancies.TryGetValue(key, out var vehicles))
                {
                    if (vehicles.Remove(vehicleCode))
                    {
                        _logger.LogInformation("批次 [{Batch}] 区域 [{Zone}] 已释放车辆 [{Vehicle}] 通行许可 (剩余占用 {Count})",
                            batchCode, zoneOrStation, vehicleCode, vehicles.Count);
                    }

                    if (vehicles.Count == 0)
                    {
                        _activeZoneOccupancies.TryRemove(key, out _);
                    }
                }
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<bool> CheckConvergenceAsync(
            string batchCode,
            int targetStepIndex,
            CancellationToken cancellationToken = default)
        {
            Check.NotNullOrWhiteSpace(batchCode, nameof(batchCode));

            // 查询该批次下所有属于该批次的活动子任务（排除已取消或已终态的任务）
            var subTasks = await _taskRepository.GetListAsync(
                t => t.BatchId == batchCode,
                cancellationToken: cancellationToken);

            if (subTasks.Count == 0)
            {
                _logger.LogWarning("批次 [{Batch}] 未找到任何关联子任务，默认汇聚成功", batchCode);
                return true;
            }

            // 检查是否有仍在运行且未达到 targetStepIndex 的任务
            var pendingSubTasks = subTasks.Where(t =>
                t.Status == AgvTaskStatus.Running && t.StepIndex < targetStepIndex).ToList();

            if (pendingSubTasks.Count > 0)
            {
                _logger.LogDebug("批次 [{Batch}] 仍有 {Count} 个子任务未到达步骤 {StepIndex}，汇聚等待中",
                    batchCode, pendingSubTasks.Count, targetStepIndex);
                return false;
            }

            _logger.LogInformation("批次 [{Batch}] 下所有运行中子任务均已推进至步骤 {StepIndex} 或完成，汇聚握手成功",
                batchCode, targetStepIndex);
            return true;
        }

        private static string BuildGateKey(string batchCode, string zoneOrStation) =>
            $"{batchCode}:{zoneOrStation}".ToUpperInvariant();
    }
}
