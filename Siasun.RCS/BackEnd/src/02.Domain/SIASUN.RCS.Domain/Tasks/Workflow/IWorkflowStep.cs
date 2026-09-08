using System;
using System.Threading.Tasks;

namespace SIASUN.RCS.Tasks.Workflow
{
    /// <summary>
    /// 工作流单步执行标准契约
    /// </summary>
    public interface IWorkflowStep
    {
        /// <summary>
        /// 步骤序号（严格对应 AgvTask.StepIndex）
        /// </summary>
        int StepIndex { get; }

        /// <summary>
        /// 步骤名称描述（如 CheckPlcSlot, DispatchTmFetch, LiftCarrier）
        /// </summary>
        string StepName { get; }

        /// <summary>
        /// 所属活动程段（如 Fetch, Put, Park）
        /// </summary>
        string ActiveLeg { get; }

        /// <summary>
        /// 是否为可幂等操作（查询/握手/开门为 true，机械臂举升/放货等物理动作严禁设为 true）
        /// </summary>
        bool IsIdempotent { get; }

        /// <summary>
        /// 单步执行超时时限（默认为 30 秒）
        /// </summary>
        TimeSpan Timeout { get; }

        /// <summary>
        /// 执行该步骤核心逻辑
        /// </summary>
        /// <param name="context">工作流上下文</param>
        /// <returns>执行结果</returns>
        Task<StepExecutionResult> ExecuteAsync(WorkflowStepContext context);

        /// <summary>
        /// SAGA 补偿逆向操作（当后续步骤发生不可逆异常需要步退回滚时调用）
        /// </summary>
        /// <param name="context">工作流上下文</param>
        /// <returns>异步任务</returns>
        Task CompensateAsync(WorkflowStepContext context);
    }
}
