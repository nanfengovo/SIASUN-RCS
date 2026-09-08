using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace SIASUN.RCS.Tasks
{
    /// <summary>
    /// TM 底层序列号注册中心实现（严格落地选项 B：内存 ConcurrentDictionary 快路径 + EF Core 物理表持久化）
    /// </summary>
    public class TaskSerialRegistry : ITaskSerialRegistry
    {
        private readonly ConcurrentDictionary<string, TaskSerialMapping> _cache = new(StringComparer.OrdinalIgnoreCase);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IGuidGenerator _guidGenerator;
        private readonly ILogger<TaskSerialRegistry> _logger;

        /// <summary>
        /// 构造函数，注入 Scope 工厂、Guid 生成器与日志组件
        /// </summary>
        public TaskSerialRegistry(
            IServiceScopeFactory scopeFactory,
            IGuidGenerator guidGenerator,
            ILogger<TaskSerialRegistry> logger)
        {
            _scopeFactory = scopeFactory;
            _guidGenerator = guidGenerator;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<TaskSerialMapping> RegisterAsync(
            Guid taskId,
            string taskCode,
            string tmSerial,
            string leg,
            int stepIndex,
            string? waitingEvent = null,
            string? vehicleCode = null,
            CancellationToken cancellationToken = default)
        {
            Check.NotNullOrWhiteSpace(taskCode, nameof(taskCode));
            Check.NotNullOrWhiteSpace(tmSerial, nameof(tmSerial));
            Check.NotNullOrWhiteSpace(leg, nameof(leg));

            var mapping = new TaskSerialMapping(
                _guidGenerator.Create(),
                taskId,
                taskCode,
                tmSerial,
                leg,
                stepIndex,
                waitingEvent,
                vehicleCode);

            // 1. 写入内存高速缓存
            _cache[tmSerial] = mapping;

            // 2. 异步持久化到 EF Core 物理表
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IRepository<TaskSerialMapping, Guid>>();
                await repository.InsertAsync(mapping, autoSave: true, cancellationToken: cancellationToken);

                _logger.LogInformation("成功注册并持久化 TM 序列号映射: [TmSerial={TmSerial}] -> [Task={TaskCode}, Leg={Leg}, Step={StepIndex}, Wait={WaitingEvent}]",
                    tmSerial, taskCode, leg, stepIndex, waitingEvent ?? "None");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "持久化 TM 序列号映射失败: [TmSerial={TmSerial}, Task={TaskCode}]: {Message}", tmSerial, taskCode, ex.Message);
                // 内存已有缓存，不阻塞核心调度执行
            }

            return mapping;
        }

        /// <inheritdoc />
        public async Task<TaskSerialMapping?> FindByTmSerialAsync(string tmSerial, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tmSerial))
            {
                return null;
            }

            // 1. 高速内存快路径
            if (_cache.TryGetValue(tmSerial, out var cachedMapping))
            {
                return cachedMapping;
            }

            // 2. 内存 Miss 时回查数据库（应对服务重启、工控机崩溃自愈场景）
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IRepository<TaskSerialMapping, Guid>>();
                var dbMapping = await repository.FindAsync(x => x.TmSerial == tmSerial, cancellationToken: cancellationToken);

                if (dbMapping != null)
                {
                    _cache[tmSerial] = dbMapping;
                    _logger.LogInformation("从物理表恢复 TM 序列号映射至内存快路径: [TmSerial={TmSerial}, Task={TaskCode}]", tmSerial, dbMapping.TaskCode);
                    return dbMapping;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "回查物理表 TM 序列号映射异常: [TmSerial={TmSerial}]: {Message}", tmSerial, ex.Message);
            }

            return null;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<TaskSerialMapping>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            // 内存搜索
            var fromCache = _cache.Values.Where(x => x.TaskId == taskId).ToList();
            if (fromCache.Count > 0)
            {
                return fromCache;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IRepository<TaskSerialMapping, Guid>>();
                var list = await repository.GetListAsync(x => x.TaskId == taskId, cancellationToken: cancellationToken);

                foreach (var item in list)
                {
                    _cache[item.TmSerial] = item;
                }

                return list;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "查询任务序列号映射异常: [TaskId={TaskId}]: {Message}", taskId, ex.Message);
                return Array.Empty<TaskSerialMapping>();
            }
        }

        /// <inheritdoc />
        public async Task RemoveByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            var keysToRemove = _cache.Values
                .Where(x => x.TaskId == taskId)
                .Select(x => x.TmSerial)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IRepository<TaskSerialMapping, Guid>>();
                await repository.DeleteAsync(x => x.TaskId == taskId, autoSave: true, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "物理表清理任务序列号映射异常: [TaskId={TaskId}]: {Message}", taskId, ex.Message);
            }
        }
    }
}
