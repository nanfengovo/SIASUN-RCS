using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Volo.Abp;

namespace SIASUN.RCS.Tasks.Workflow
{
    /// <summary>
    /// 声明式工作流 JSON 序列化、反序列化与结构规范校验器
    /// </summary>
    public static class WorkflowJsonParser
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };

        /// <summary>
        /// 从 JSON 文本反序列化为 WorkflowDefinition 对象，并执行完整性与步骤连续性校验
        /// </summary>
        /// <param name="json">工作流 JSON 内容</param>
        /// <returns>已通过校验的工作流定义实体</returns>
        public static WorkflowDefinition Parse(string json)
        {
            Check.NotNullOrWhiteSpace(json, nameof(json));

            WorkflowDefinition? definition;
            try
            {
                definition = JsonSerializer.Deserialize<WorkflowDefinition>(json, Options);
            }
            catch (JsonException ex)
            {
                throw new BusinessException("RCS:InvalidWorkflowJson", innerException: ex)
                    .WithData("Json", json);
            }

            if (definition == null)
            {
                throw new BusinessException("RCS:InvalidWorkflowJson")
                    .WithData("Json", json);
            }

            Validate(definition);

            return definition;
        }

        /// <summary>
        /// 将 WorkflowDefinition 序列化为格式化 JSON 文本
        /// </summary>
        /// <param name="definition">工作流定义</param>
        /// <returns>JSON 字符串</returns>
        public static string Serialize(WorkflowDefinition definition)
        {
            Check.NotNull(definition, nameof(definition));
            return JsonSerializer.Serialize(definition, Options);
        }

        /// <summary>
        /// 校验工作流定义的完备性、步骤序号连续性与合法性
        /// </summary>
        /// <param name="definition">待校验定义</param>
        public static void Validate(WorkflowDefinition definition)
        {
            Check.NotNull(definition, nameof(definition));

            if (string.IsNullOrWhiteSpace(definition.WorkflowId))
            {
                throw new BusinessException("RCS:WorkflowValidationFailed")
                    .WithData("Reason", "WorkflowId 不能为空");
            }

            if (definition.Version < 1)
            {
                throw new BusinessException("RCS:WorkflowValidationFailed")
                    .WithData("Reason", "Version 必须大于等于 1");
            }

            if (definition.Steps == null || definition.Steps.Count == 0)
            {
                throw new BusinessException("RCS:WorkflowValidationFailed")
                    .WithData("Reason", "工作流步骤列表不能为空");
            }

            // 检查 StepIndex 重复
            var duplicateIndex = definition.Steps
                .GroupBy(s => s.StepIndex)
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicateIndex != null)
            {
                throw new BusinessException("RCS:WorkflowValidationFailed")
                    .WithData("Reason", $"存在重复的步骤索引 StepIndex={duplicateIndex.Key}");
            }

            // 步骤按 StepIndex 升序排序
            definition.Steps = definition.Steps.OrderBy(s => s.StepIndex).ToList();

            for (var i = 0; i < definition.Steps.Count; i++)
            {
                var step = definition.Steps[i];
                if (step.StepIndex != i)
                {
                    throw new BusinessException("RCS:WorkflowValidationFailed")
                        .WithData("Reason", $"步骤索引必须从 0 开始且连续单调递增，当前第 {i} 项的 StepIndex 为 {step.StepIndex}");
                }

                if (string.IsNullOrWhiteSpace(step.StepName))
                {
                    throw new BusinessException("RCS:WorkflowValidationFailed")
                        .WithData("Reason", $"第 {step.StepIndex} 步的 StepName 不能为空");
                }

                if (string.IsNullOrWhiteSpace(step.StepType))
                {
                    throw new BusinessException("RCS:WorkflowValidationFailed")
                        .WithData("Reason", $"第 {step.StepIndex} 步的 StepType 不能为空");
                }
            }
        }
    }
}
