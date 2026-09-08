using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SIASUN.RCS.Tasks;

namespace SIASUN.RCS.Batches
{
    /// <summary>
    /// 批次管理与多车编排领域服务契约
    /// </summary>
    public interface IBatchOrchestrator
    {
        /// <summary>
        /// 将批次分解拆分为单车搬运子任务列表并绑定载具与批次编号
        /// </summary>
        /// <param name="batch">批次聚合根</param>
        /// <param name="carrierCodes">载具清单</param>
        /// <param name="workflowKey">选用的工作流模板</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>拆分生成的待执行任务列表</returns>
        Task<IReadOnlyList<AgvTask>> DecomposeBatchAsync(
            AgvBatch batch,
            IReadOnlyList<string> carrierCodes,
            string workflowKey = "transfer_standard",
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查批次下各子任务的执行进度并更新批次聚合根的汇聚状态
        /// </summary>
        /// <param name="batch">批次聚合根</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>批次是否已全部汇聚完成</returns>
        Task<bool> EvaluateConvergenceAsync(
            AgvBatch batch,
            CancellationToken cancellationToken = default);
    }
}
