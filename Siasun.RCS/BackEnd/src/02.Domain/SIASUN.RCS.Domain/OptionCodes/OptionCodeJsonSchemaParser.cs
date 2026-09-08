using System.Text.Json;
using System.Text.Json.Serialization;
using Volo.Abp;

namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// OptionCode Schema JSON 序列化与反序列化解析器
    /// </summary>
    public static class OptionCodeJsonSchemaParser
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
        /// 从 JSON 字符串解析为 OptionCodeSchemaDefinition 对象
        /// </summary>
        /// <param name="json">JSON 文本</param>
        /// <returns>Schema 实体定义</returns>
        public static OptionCodeSchemaDefinition Parse(string json)
        {
            Check.NotNullOrWhiteSpace(json, nameof(json));

            try
            {
                var schema = JsonSerializer.Deserialize<OptionCodeSchemaDefinition>(json, Options);
                if (schema == null)
                {
                    throw new BusinessException("RCS:InvalidOptionCodeSchemaJson")
                        .WithData("Json", json);
                }

                return schema;
            }
            catch (JsonException ex)
            {
                throw new BusinessException("RCS:InvalidOptionCodeSchemaJson", innerException: ex)
                    .WithData("Json", json);
            }
        }

        /// <summary>
        /// 将 OptionCodeSchemaDefinition 序列化为格式化 JSON 字符串
        /// </summary>
        /// <param name="schema">Schema 定义</param>
        /// <returns>JSON 字符串</returns>
        public static string Serialize(OptionCodeSchemaDefinition schema)
        {
            Check.NotNull(schema, nameof(schema));
            return JsonSerializer.Serialize(schema, Options);
        }
    }
}

