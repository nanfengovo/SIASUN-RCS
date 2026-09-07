using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SIASUN.RCS.Locations
{
    /// <summary>
    /// 物理空间与库位原子锁领域服务契约
    /// 调度前置安全区：保证物理搬运无冲突与防死锁有序预占
    /// </summary>
    public interface ILocationLocker
    {
        /// <summary>
        /// 原子批量申请库位锁（严格按编码升序抢占，彻底杜绝死锁，全成功或全回滚）
        /// </summary>
        /// <param name="taskId">申请任务主键</param>
        /// <param name="vehicleCode">执行车辆编号</param>
        /// <param name="requests">申请锁意图集合</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>锁定结果（成功或首个冲突库位详情）</returns>
        Task<LocationLockResult> TryAcquireLocksAsync(
            Guid taskId,
            string vehicleCode,
            IEnumerable<LocationLockRequest> requests,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 提前释放单库位锁（Fetch 离开源库位时提前释放，提升立库吞吐效率）
        /// </summary>
        /// <param name="locationCode">库位编码</param>
        /// <param name="taskId">持锁任务ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task ReleaseLockAsync(
            string locationCode,
            Guid taskId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 释放指定任务持有的全部库位锁（任务生命周期终态 Succeeded/Failed/Canceled 后的兜底释放）
        /// </summary>
        /// <param name="taskId">任务主键</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task ReleaseAllByTaskAsync(
            Guid taskId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 心跳续约任务持有的全部库位锁（防止长距行驶或避障挂起时被系统自愈抢占）
        /// </summary>
        /// <param name="taskId">任务主键</param>
        /// <param name="extensionDuration">延长时长</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否存在被续约的锁记录</returns>
        Task<bool> RenewLeasesAsync(
            Guid taskId,
            TimeSpan extensionDuration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 调度员人工维护封锁库位（禁止任何调度任务抢占，无限期租约）
        /// </summary>
        /// <param name="locationCode">库位编码</param>
        /// <param name="reason">维护封锁原因</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否成功加维护锁（若已被作业任务占领则返回 false）</returns>
        Task<bool> LockForMaintenanceAsync(
            string locationCode,
            string reason,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 调度员人工解除维护封锁
        /// </summary>
        /// <param name="locationCode">库位编码</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否成功解封</returns>
        Task<bool> UnlockMaintenanceAsync(
            string locationCode,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 调度员强制解除任何持有者的库位锁（事故强行脱困干预）
        /// </summary>
        /// <param name="locationCode">库位编码</param>
        /// <param name="reason">强制解锁原因</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>原持锁任务ID（若有）</returns>
        Task<Guid?> ForceUnlockAsync(
            string locationCode,
            string reason,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 快速检查库位是否已被锁定（含作业锁与维护锁）
        /// </summary>
        /// <param name="locationCode">库位编码</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否已锁定</returns>
        Task<bool> IsLockedAsync(
            string locationCode,
            CancellationToken cancellationToken = default);
    }
}
