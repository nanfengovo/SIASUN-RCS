using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Diagnostics
{
    /// <summary>
    /// 基于单机进程内信号量的诊断排他锁实现（单机轻量部署与默认本地实现）
    /// 支持在未配置 Redis 等外部中间件时作为默认 Seam 保证节点内任务互斥
    /// </summary>
    [Dependency(TryRegister = true)]
    public class LocalDiagnosticLock : IDistributedDiagnosticLock, ISingletonDependency
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 尝试获取指定资源的排他锁
        /// </summary>
        /// <param name="resourceKey">受控资源标识</param>
        /// <param name="timeout">最大等待超时</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>若成功获取锁则返回 IDisposable 释放器，超时返回 null</returns>
        public async Task<IDisposable?> TryAcquireLockAsync(
            string resourceKey,
            TimeSpan timeout = default,
            CancellationToken cancellationToken = default)
        {
            var sem = _locks.GetOrAdd(resourceKey, _ => new SemaphoreSlim(1, 1));
            var acquired = await sem.WaitAsync(timeout, cancellationToken);
            if (!acquired)
            {
                return null;
            }

            return new LockReleaser(sem);
        }

        /// <summary>
        /// 检查指定资源当前是否已被加锁
        /// </summary>
        /// <param name="resourceKey">受控资源标识</param>
        /// <returns>是否锁定</returns>
        public Task<bool> IsLockedAsync(string resourceKey)
        {
            if (_locks.TryGetValue(resourceKey, out var sem))
            {
                return Task.FromResult(sem.CurrentCount == 0);
            }

            return Task.FromResult(false);
        }

        private class LockReleaser : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;
            private int _disposed;

            public LockReleaser(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                {
                    _semaphore.Release();
                }
            }
        }
    }
}
