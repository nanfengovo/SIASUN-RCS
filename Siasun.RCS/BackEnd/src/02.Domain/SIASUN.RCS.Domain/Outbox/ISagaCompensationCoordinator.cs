using System;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Outbox
{
    /// <summary>
    /// SAGA 业务补偿协调器契约
    /// 确保在非幂等动作失败或执行步进回滚时，跨外部系统（MES/WMS/机台）的补偿动作通过 Outbox 达成可靠一致性
    /// </summary>
    public interface ISagaCompensationCoordinator : ITransientDependency
    {
        /// <summary>
        /// 触发 SAGA 补偿流程（入队 Outbox 事务补偿报文并抛出领域补偿事件）
        /// </summary>
        Task TriggerCompensationAsync(
            Guid taskId,
            string taskCode,
            int failedStepIndex,
            string failureReason,
            string compensationAction,
            string? destination = null,
            string? traceId = null,
            CancellationToken cancellationToken = default);
    }
}
