using System.Collections.Generic;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Tasks.Workflow
{
    /// <summary>
    /// 声明式工作流定义统一注册与版本检索中心接口
    /// </summary>
    public interface IWorkflowDefinitionRegistry : ISingletonDependency
    {
        /// <summary>
        /// 注册或覆盖一个工作流定义
        /// </summary>
        /// <param name="definition">工作流定义</param>
        void Register(WorkflowDefinition definition);

        /// <summary>
        /// 查找指定代号与版本的工作流（未找到返回 null）
        /// </summary>
        /// <param name="workflowId">工作流代号（如 "transfer_standard"、"erack_docking"）</param>
        /// <param name="version">版本号（若为 null，默认返回最新最高版本）</param>
        /// <returns>工作流定义或 null</returns>
        WorkflowDefinition? Find(string workflowId, int? version = null);

        /// <summary>
        /// 获取指定代号与版本的工作流（未找到抛出 BusinessException）
        /// </summary>
        /// <param name="workflowId">工作流代号</param>
        /// <param name="version">版本号（若为 null，默认返回最新最高版本）</param>
        /// <returns>工作流定义</returns>
        WorkflowDefinition Get(string workflowId, int? version = null);

        /// <summary>
        /// 获取所有已注册的工作流定义列表
        /// </summary>
        /// <returns>工作流定义列表</returns>
        IReadOnlyList<WorkflowDefinition> GetAll();
    }
}
