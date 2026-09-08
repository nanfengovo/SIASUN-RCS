using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// 基于本地物理目录扫描与 JSON 解析的 OptionCode 配置文件加载器
    /// </summary>
    public class OptionCodeSchemaFileLoader : IOptionCodeSchemaLoader, ITransientDependency
    {
        private readonly IOptionCodeSchemaRegistry _registry;
        private readonly ILogger<OptionCodeSchemaFileLoader> _logger;

        /// <summary>
        /// 构造函数，注入注册中心与日志
        /// </summary>
        public OptionCodeSchemaFileLoader(
            IOptionCodeSchemaRegistry registry,
            ILogger<OptionCodeSchemaFileLoader> logger)
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
                var candidate = Path.Combine(baseDir, "OptionCodeSchemas");
                if (Directory.Exists(candidate))
                {
                    targetDir = candidate;
                }
                else
                {
                    // 开发环境源码目录探测
                    var srcCandidate = Path.Combine(Directory.GetCurrentDirectory(), "OptionCodeSchemas");
                    if (Directory.Exists(srcCandidate))
                    {
                        targetDir = srcCandidate;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(targetDir) || !Directory.Exists(targetDir))
            {
                _logger.LogWarning("OptionCode 配置文件目录未找到: {Directory}", targetDir ?? "null");
                return 0;
            }

            var jsonFiles = Directory.GetFiles(targetDir, "*.json", SearchOption.TopDirectoryOnly);
            var loadedCount = 0;

            foreach (var filePath in jsonFiles)
            {
                try
                {
                    var jsonContent = File.ReadAllText(filePath);
                    var schema = OptionCodeJsonSchemaParser.Parse(jsonContent);
                    _registry.Register(schema);
                    loadedCount++;
                    _logger.LogInformation("成功加载并注册 OptionCode Schema: [{FullKey}] - {Title} (文件: {File})",
                        schema.FullKey, schema.Title, Path.GetFileName(filePath));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "解析 OptionCode 配置文件失败: {FilePath}", filePath);
                }
            }

            _logger.LogInformation("OptionCode Schema 文件扫描完成，共成功加载 {Count} 个现场位图配置", loadedCount);
            return loadedCount;
        }
    }
}

