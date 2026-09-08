using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Tasks
{
    /// <summary>
    /// TM 底层序列号注册中心接口（统一双向映射，消除字符串切分 hack，支持内存快速查询与 EF Core 物理表持久化自愈）
    /// </summary>
    public interface ITaskSerialRegistry : ISingletonDependency
    {
        /// <summary>
        /// 注册一个 TM 报文序列号映射
        /// </summary>
        Task<TaskSerialMapping> RegisterAsync(
            Guid taskId,
            string taskCode,
            string tmSerial,
            string leg,
            int stepIndex,
            string? waitingEvent = null,
            string? vehicleCode = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据 TM 报文流水号检索关联映射（内存优先，Miss 则回查数据库）
        /// </summary>
        Task<TaskSerialMapping?> FindByTmSerialAsync(string tmSerial, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取指定内部任务绑定的所有程段序列号映射
        /// </summary>
        Task<IReadOnlyList<TaskSerialMapping>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 清理指定任务的所有序列号映射记录
        /// </summary>
        Task RemoveByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);
    }
}
