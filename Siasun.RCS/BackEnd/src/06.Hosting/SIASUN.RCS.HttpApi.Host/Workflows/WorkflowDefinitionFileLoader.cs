using System;
using System.IO;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Tasks.Workflow;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Workflows
{
    /// <summary>
    /// 基于本地物理目录扫描与 JSON 解析的声明式工作流配置文件加载器
    /// </summary>
    public class WorkflowDefinitionFileLoader : IWorkflowDefinitionLoader, ITransientDependency
    {
        private readonly IWorkflowDefinitionRegistry _registry;
        private readonly ILogger<WorkflowDefinitionFileLoader> _logger;

        /// <summary>
        /// 构造函数注入注册中心与日志组件
        /// </summary>
        public WorkflowDefinitionFileLoader(
            IWorkflowDefinitionRegistry registry,
            ILogger<WorkflowDefinitionFileLoader> logger)
        {
            _registry = registry;
            _logger = logger;
        }

        /// <inheritdoc />
        public int LoadFromDirectory(string? directoryPath = null)
        {
            var targetDir = directoryPath;

            if (string.IsNullOrWhiteSpace(targetDir))
            {
                var baseDir = AppContext.BaseDirectory;
                var candidate = Path.Combine(baseDir, "WorkflowSchemas");
                if (Directory.Exists(candidate))
                {
                    targetDir = candidate;
                }
                else
                {
                    // 开发环境源码目录探测
                    var srcCandidate = Path.Combine(Directory.GetCurrentDirectory(), "WorkflowSchemas");
                    if (Directory.Exists(srcCandidate))
                    {
                        targetDir = srcCandidate;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(targetDir) || !Directory.Exists(targetDir))
            {
                _logger.LogWarning("WorkflowSchemas 配置文件目录未找到: {Directory}", targetDir ?? "null");
                return 0;
            }

            var jsonFiles = Directory.GetFiles(targetDir, "*.json", SearchOption.TopDirectoryOnly);
            var loadedCount = 0;

            foreach (var filePath in jsonFiles)
            {
                try
                {
                    var jsonContent = File.ReadAllText(filePath);
                    var definition = WorkflowJsonParser.Parse(jsonContent);
                    _registry.Register(definition);
                    loadedCount++;
                    _logger.LogInformation("成功加载并注册任务工作流 Schema: [{FullKey}] - {Title} (文件: {File}, 步骤数: {StepCount})",
                        definition.FullKey, definition.Title, Path.GetFileName(filePath), definition.Steps.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "解析工作流配置文件失败: {FilePath}", filePath);
                }
            }

            _logger.LogInformation("Workflow Schema 文件扫描完成，共成功加载 {Count} 个现场工作流配置", loadedCount);
            return loadedCount;
        }
    }
}
