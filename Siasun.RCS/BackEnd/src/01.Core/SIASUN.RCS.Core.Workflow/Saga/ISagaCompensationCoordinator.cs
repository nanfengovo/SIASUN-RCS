using System;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Tasks.Workflow.Saga
{
    /// <summary>
    /// SAGA 四级逆向事务补偿器契约
    /// </summary>
    public interface ISagaCompensationCoordinator : ITransientDependency
    {
        /// <summary>
        /// 执行 SAGA 分级逆向补偿处理
        /// </summary>
        /// <param name="task">任务工作流契约</param>
        /// <param name="level">补偿级别</param>
        /// <param name="targetStepIndex">目标安全步索引（Level 2 时使用）</param>
        /// <param name="reason">补偿触发原因</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>补偿处理结果</returns>
        Task<bool> CompensateAsync(
            IWorkflowTask task,
            SagaCompensationLevel level,
            int targetStepIndex = 0,
            string reason = "异常自动补偿",
            CancellationToken cancellationToken = default);
    }
}
