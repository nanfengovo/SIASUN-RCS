using System;
using System.Threading;
using System.Threading.Tasks;

namespace SIASUN.RCS.Diagnostics
{
    /// <summary>
    /// 分布式诊断与运维排他锁接口（L4 多实例/集群一致性 Seam）
    /// 确保在多节点高可用部署下，磁盘自愈清理、日志归档与黑匣子导出等关键后台任务互斥执行，避免争抢
    /// </summary>
    public interface IDistributedDiagnosticLock
    {
        /// <summary>
        /// 尝试获取指定资源的分布式诊断排他锁
        /// </summary>
        /// <param name="resourceKey">受控资源标识（如 "rcs:lock:disk-self-heal"）</param>
        /// <param name="timeout">获取锁的最大等待超时时间</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>若成功获取锁则返回持锁凭证（释放时自动解锁），若超时未获取则返回 null</returns>
        Task<IDisposable?> TryAcquireLockAsync(
            string resourceKey,
            TimeSpan timeout = default,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查指定资源当前是否已被任何节点加锁
        /// </summary>
        /// <param name="resourceKey">受控资源标识</param>
        /// <returns>是否处于锁定状态</returns>
        Task<bool> IsLockedAsync(string resourceKey);
    }
}
