using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;

namespace SIASUN.RCS.Tasks.Workflow
{
    /// <summary>
    /// 声明式工作流注册与检索中心默认实现（保持领域层纯净，无硬编码客户现场流程）
    /// </summary>
    public class WorkflowDefinitionRegistry : IWorkflowDefinitionRegistry
    {
        private readonly ConcurrentDictionary<string, WorkflowDefinition> _definitions = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 默认无参构造函数
        /// </summary>
        public WorkflowDefinitionRegistry()
        {
        }

        /// <summary>
        /// 接收外部初始工作流集合的构造函数
        /// </summary>
        /// <param name="definitions">工作流定义集合</param>
        public WorkflowDefinitionRegistry(IEnumerable<WorkflowDefinition>? definitions)
        {
            if (definitions != null)
            {
                foreach (var def in definitions)
                {
                    Register(def);
                }
            }
        }

        /// <inheritdoc />
        public void Register(WorkflowDefinition definition)
        {
            Check.NotNull(definition, nameof(definition));
            Check.NotNullOrWhiteSpace(definition.WorkflowId, nameof(definition.WorkflowId));

            _definitions[definition.FullKey] = definition;
        }

        /// <inheritdoc />
        public WorkflowDefinition? Find(string workflowId, int? version = null)
        {
            if (string.IsNullOrWhiteSpace(workflowId))
            {
                return null;
            }

            var cleanId = workflowId.Trim().ToLowerInvariant();

            // 如果包含版本后缀，如 "transfer_standard.v1"
            if (cleanId.Contains(".v"))
            {
                if (_definitions.TryGetValue(cleanId, out var exactDef))
                {
                    return exactDef;
                }

                var parts = cleanId.Split(".v", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && int.TryParse(parts[1], out var parsedVer))
                {
                    cleanId = parts[0];
                    version ??= parsedVer;
                }
            }

            if (version.HasValue)
            {
                var fullKey = $"{cleanId}.v{version.Value}";
                return _definitions.TryGetValue(fullKey, out var def) ? def : null;
            }

            // 若未指定版本，返回该 workflowId 下版本号最大的定义
            return _definitions.Values
                .Where(d => string.Equals(d.WorkflowId, cleanId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(d => d.Version)
                .FirstOrDefault();
        }

        /// <inheritdoc />
        public WorkflowDefinition Get(string workflowId, int? version = null)
        {
            var def = Find(workflowId, version);
            if (def == null)
            {
                throw new BusinessException("RCS:WorkflowDefinitionNotFound")
                    .WithData("WorkflowId", workflowId)
                    .WithData("Version", version?.ToString() ?? "latest");
            }

            return def;
        }

        /// <inheritdoc />
        public IReadOnlyList<WorkflowDefinition> GetAll()
        {
            return _definitions.Values
                .OrderBy(d => d.WorkflowId)
                .ThenByDescending(d => d.Version)
                .ToList();
        }
    }
}
