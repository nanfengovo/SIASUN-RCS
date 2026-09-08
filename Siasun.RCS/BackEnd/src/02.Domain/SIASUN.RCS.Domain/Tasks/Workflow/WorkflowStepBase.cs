using System;
using System.Threading.Tasks;

namespace SIASUN.RCS.Tasks.Workflow
{
    /// <summary>
    /// 工作流步骤抽象基类，提供默认超时与空补偿实现
    /// </summary>
    public abstract class WorkflowStepBase : IWorkflowStep
    {
        /// <inheritdoc />
        public abstract int StepIndex { get; }

        /// <inheritdoc />
        public abstract string StepName { get; }

        /// <inheritdoc />
        public virtual string ActiveLeg => "Default";

        /// <inheritdoc />
        public virtual bool IsIdempotent => false;

        /// <inheritdoc />
        public virtual TimeSpan Timeout => TimeSpan.FromSeconds(30);

        /// <inheritdoc />
        public abstract Task<StepExecutionResult> ExecuteAsync(WorkflowStepContext context);

        /// <inheritdoc />
        public virtual Task CompensateAsync(WorkflowStepContext context)
        {
            // 默认无逆向副作用补偿
            return Task.CompletedTask;
        }
    }
}
