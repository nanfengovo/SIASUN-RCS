using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SIASUN.RCS.Tasks.Workflow
{
    /// <summary>
    /// 轻量步进式 TaskWorkflow 引擎契约
    /// 驱动 AgvTask 5 状态粗粒度生命周期下的细粒度 StepIndex 与异步事件唤醒
    /// </summary>
    public interface ITaskWorkflowEngine
    {
        /// <summary>
        /// 注册指定类型的工作流步骤集合
        /// </summary>
        /// <param name="workflowKey">工作流类型标识（例如 DefaultTransport, SemiconductorWafer）</param>
        /// <param name="steps">步骤流水线集合</param>
        void RegisterWorkflow(string workflowKey, IEnumerable<IWorkflowStep> steps);

        /// <summary>
        /// 获取指定工作流当前已注册的所有步骤
        /// </summary>
        /// <param name="workflowKey">工作流类型标识</param>
        /// <returns>有序步骤列表</returns>
        IReadOnlyList<IWorkflowStep> GetSteps(string workflowKey);

        /// <summary>
        /// 驱动执行任务当前 StepIndex 对应的步骤
        /// </summary>
        /// <param name="task">任务聚合根</param>
        /// <param name="workflowKey">工作流类型标识（默认为 "Default"）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否已成功推进或结束</returns>
        Task<StepExecutionResult> ExecuteCurrentStepAsync(AgvTask task, string workflowKey = "Default", CancellationToken cancellationToken = default);

        /// <summary>
        /// 接收外部异步信号唤醒挂起中的任务步骤，并驱动后续步进
        /// </summary>
        /// <param name="task">任务聚合根</param>
        /// <param name="signalEvent">收到的外部事件标识（如 TM 报文指令完成、PLC 安全门已开）</param>
        /// <param name="payload">外部携带的数据报文</param>
        /// <param name="workflowKey">工作流类型标识</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>推进结果</returns>
        Task<StepExecutionResult> ResumeBySignalAsync(AgvTask task, string signalEvent, object? payload = null, string workflowKey = "Default", CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行 SAGA 补偿回退（向后倒序执行 CompensateAsync，直至回退到目标安全步）
        /// </summary>
        /// <param name="task">任务聚合根</param>
        /// <param name="targetStepIndex">回退的目标步骤</param>
        /// <param name="reason">回退原因</param>
        /// <param name="workflowKey">工作流类型标识</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否回退成功</returns>
        Task<bool> RollbackToStepAsync(AgvTask task, int targetStepIndex, string reason, string workflowKey = "Default", CancellationToken cancellationToken = default);
    }
}
