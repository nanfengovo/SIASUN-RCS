using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;

namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// OptionCode Schema 注册与检索中心默认实现（保持领域层纯净，无硬编码客户现场）
    /// </summary>
    public class OptionCodeSchemaRegistry : IOptionCodeSchemaRegistry
    {
        private readonly ConcurrentDictionary<string, OptionCodeSchemaDefinition> _schemas = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 默认无参构造函数（纯净内核，不包含任何特异客户预置）
        /// </summary>
        public OptionCodeSchemaRegistry()
        {
        }

        /// <summary>
        /// 接收外部注入 Schema 集合的构造函数
        /// </summary>
        /// <param name="schemas">待加载的 Schema 列表</param>
        public OptionCodeSchemaRegistry(IEnumerable<OptionCodeSchemaDefinition> schemas)
        {
            if (schemas != null)
            {
                foreach (var schema in schemas)
                {
                    Register(schema);
                }
            }
        }

        /// <inheritdoc />
        public void Register(OptionCodeSchemaDefinition schema)
        {
            Check.NotNull(schema, nameof(schema));
            Check.NotNullOrWhiteSpace(schema.Code, nameof(schema.Code));

            _schemas[schema.FullKey] = schema;
        }

        /// <inheritdoc />
        public OptionCodeSchemaDefinition? Find(string code, int? version = null)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            var cleanCode = code.Trim().ToLowerInvariant();

            // 如果包含版本后缀，如 "txc_demo.v1"
            if (cleanCode.Contains(".v"))
            {
                if (_schemas.TryGetValue(cleanCode, out var exactSchema))
                {
                    return exactSchema;
                }

                // 拆解 code 与 version
                var parts = cleanCode.Split(".v", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && int.TryParse(parts[1], out var parsedVer))
                {
                    cleanCode = parts[0];
                    version ??= parsedVer;
                }
            }

            if (version.HasValue)
            {
                var fullKey = $"{cleanCode}.v{version.Value}";
                return _schemas.TryGetValue(fullKey, out var schema) ? schema : null;
            }

            // 若未指定版本，查找该 code 下版本号最大的 Schema
            return _schemas.Values
                .Where(s => string.Equals(s.Code, cleanCode, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(s => s.Version)
                .FirstOrDefault();
        }

        /// <inheritdoc />
        public OptionCodeSchemaDefinition Get(string code, int? version = null)
        {
            var schema = Find(code, version);
            if (schema == null)
            {
                throw new BusinessException("RCS:OptionCodeSchemaNotFound")
                    .WithData("Code", code)
                    .WithData("Version", version?.ToString() ?? "latest");
            }

            return schema;
        }

        /// <inheritdoc />
        public IReadOnlyList<OptionCodeSchemaDefinition> GetAll()
        {
            return _schemas.Values
                .OrderBy(s => s.Code)
                .ThenByDescending(s => s.Version)
                .ToList();
        }
    }
}
