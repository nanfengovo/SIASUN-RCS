using System;
using System.Threading;

namespace SIASUN.RCS.Tasks.Workflow
{
    /// <summary>
    /// 工作流步骤执行上下文
    /// </summary>
    public class WorkflowStepContext
    {
        /// <summary>
        /// 当前关联的调度任务契约
        /// </summary>
        public IWorkflowTask Task { get; }

        /// <summary>
        /// 依赖注入服务提供者（用于按需解析外部硬件适配器或仓储）
        /// </summary>
        public IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// 外部唤醒时携带的有效载荷数据（可选）
        /// </summary>
        public object? SignalPayload { get; set; }

        /// <summary>
        /// 取消令牌
        /// </summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public WorkflowStepContext(IWorkflowTask task, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            Task = task;
            ServiceProvider = serviceProvider;
            CancellationToken = cancellationToken;
        }
    }
}
