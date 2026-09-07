using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Locations.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus;

namespace SIASUN.RCS.Locations
{
    /// <summary>
    /// 基于内存并发字典高速缓存的库位点位转换解析器实现
    /// 支持微内核亚毫秒级无锁寻址，并监听 LocationMapChangedEvent 实现零延迟热重载
    /// </summary>
    public class MemoryCachedLocationMapResolver : ILocationMapResolver, ISingletonDependency, ILocalEventHandler<LocationMapChangedEvent>
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MemoryCachedLocationMapResolver> _logger;

        private readonly ConcurrentDictionary<string, LocationMapInfo> _cache =
            new ConcurrentDictionary<string, LocationMapInfo>(StringComparer.OrdinalIgnoreCase);

        private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);
        private volatile bool _isInitialized = false;

        public MemoryCachedLocationMapResolver(
            IServiceScopeFactory scopeFactory,
            ILogger<MemoryCachedLocationMapResolver> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<string?> ResolveStationCodeAsync(string locationCode, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(locationCode)) return null;

            await EnsureInitializedAsync(cancellationToken);

            if (_cache.TryGetValue(locationCode.Trim(), out var info))
            {
                return info.IsEnabled ? info.StationCode : null;
            }

            return null;
        }

        public async Task<LocationMapInfo?> GetMappingAsync(string locationCode, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(locationCode)) return null;

            await EnsureInitializedAsync(cancellationToken);

            if (_cache.TryGetValue(locationCode.Trim(), out var info))
            {
                return info.IsEnabled ? info : null;
            }

            return null;
        }

        public async Task RefreshCacheAsync(CancellationToken cancellationToken = default)
        {
            await ReloadAllAsync(cancellationToken);
        }

        public async Task HandleEventAsync(LocationMapChangedEvent eventData)
        {
            _logger.LogInformation("收到点位映射变更领域事件 [{ChangeType}]，库位: [{LocationCode}]，正在热刷新内存缓存",
                eventData.ChangeType, eventData.LocationCode);

            try
            {
                await ReloadAllAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "点位映射变更领域事件触发缓存重载发生异常");
            }
        }

        private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
        {
            if (_isInitialized) return;

            await _initLock.WaitAsync(cancellationToken);
            try
            {
                if (_isInitialized) return;
                await ReloadAllAsync(cancellationToken);
                _isInitialized = true;
            }
            finally
            {
                _initLock.Release();
            }
        }

        private async Task ReloadAllAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<LocationMap, Guid>>();

            var list = await repository.GetListAsync(cancellationToken: cancellationToken);

            _cache.Clear();
            foreach (var item in list)
            {
                var info = new LocationMapInfo(
                    item.LocationCode,
                    item.StationCode,
                    item.PreDockStationCode,
                    item.MapCode,
                    item.Heading,
                    item.Area,
                    item.IsEnabled);

                _cache[item.LocationCode] = info;
            }

            _logger.LogInformation("库位点位映射缓存重载完成，当前已加载 {Count} 条点位记录", _cache.Count);
        }
    }
}
