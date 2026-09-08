using System;
using System.Collections.Generic;

namespace SIASUN.RCS.Tasks.Workflow
{
    /// <summary>
    /// 声明式工作流 Schema 定义模型（对应现场项目的 JSON 流程配置，如 transfer_standard.v1）
    /// </summary>
    public class WorkflowDefinition
    {
        /// <summary>
        /// 工作流唯一标识代号（例如 "transfer_standard", "erack_docking"）
        /// </summary>
        public string WorkflowId { get; set; } = string.Empty;

        /// <summary>
        /// 架构版本号（从 1 开始单调递增）
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// 工作流中文可读标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 工作流详细业务与现场描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 完整检索键（例如 "transfer_standard.v1"）
        /// </summary>
        public string FullKey => $"{WorkflowId}.v{Version}";

        /// <summary>
        /// 步骤定义列表（按 StepIndex 升序执行）
        /// </summary>
        public List<WorkflowStepDefinition> Steps { get; set; } = new();

        /// <summary>
        /// 默认无参构造函数
        /// </summary>
        public WorkflowDefinition()
        {
        }

        /// <summary>
        /// 带参构造函数
        /// </summary>
        public WorkflowDefinition(string workflowId, int version, string title, string? description = null)
        {
            WorkflowId = workflowId;
            Version = version;
            Title = title;
            Description = description;
        }
    }
}
