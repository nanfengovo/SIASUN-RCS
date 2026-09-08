using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Volo.Abp;

namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// OptionCode 字段多源装配器默认实现
    /// </summary>
    public class OptionCodeAssembler : IOptionCodeAssembler
    {
        private static readonly Regex DigitsRegex = new(@"\d+", RegexOptions.Compiled);

        /// <inheritdoc />
        public Dictionary<string, Dictionary<string, uint>> Assemble(
            OptionCodeSchemaDefinition schema,
            OptionCodeAssembleContext context)
        {
            Check.NotNull(schema, nameof(schema));
            Check.NotNull(context, nameof(context));

            var result = new Dictionary<string, Dictionary<string, uint>>(StringComparer.OrdinalIgnoreCase);

            foreach (var part in schema.Parts)
            {
                var partValues = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);

                foreach (var field in part.Fields)
                {
                    var rawValue = ResolveFieldValue(field, context);

                    // 越界校验：数值不得超过该字段位宽所能表示的最大值
                    if (rawValue > field.Mask)
                    {
                        throw new BusinessException("RCS:OptionFieldValueOverflow")
                            .WithData("PartKey", part.Key)
                            .WithData("FieldKey", field.Key)
                            .WithData("Value", rawValue)
                            .WithData("MaxAllowed", field.Mask)
                            .WithData("BitWidth", field.BitWidth);
                    }

                    partValues[field.Key] = (uint)rawValue;
                }

                result[part.Key] = partValues;
            }

            return result;
        }

        private long ResolveFieldValue(OptionCodeFieldDefinition field, OptionCodeAssembleContext context)
        {
            switch (field.Source)
            {
                case OptionCodeFieldSource.Const:
                    return field.ConstValue ?? 0;

                case OptionCodeFieldSource.Args:
                    if (TryGetNumericValue(context.TaskArgs, field.Key, out var argsVal))
                    {
                        return argsVal;
                    }
                    if (field.Required)
                    {
                        throw new BusinessException("RCS:MissingRequiredOptionField")
                            .WithData("FieldKey", field.Key)
                            .WithData("Source", "Args")
                            .WithData("Label", field.Label);
                    }
                    return field.DefaultValue ?? 0;

                case OptionCodeFieldSource.Master:
                    if (TryGetNumericValue(context.MasterValues, field.Key, out var masterVal))
                    {
                        return masterVal;
                    }
                    // 支持降级到 TaskArgs
                    if (TryGetNumericValue(context.TaskArgs, field.Key, out var fallbackMasterVal))
                    {
                        return fallbackMasterVal;
                    }
                    if (field.Required)
                    {
                        throw new BusinessException("RCS:MissingRequiredOptionField")
                            .WithData("FieldKey", field.Key)
                            .WithData("Source", "Master")
                            .WithData("Label", field.Label);
                    }
                    return field.DefaultValue ?? 0;

                case OptionCodeFieldSource.Leg:
                    // 1. 优先检查 TaskArgs 是否有显式传入数值
                    if (TryGetNumericValue(context.TaskArgs, field.Key, out var explicitLegVal))
                    {
                        return explicitLegVal;
                    }

                    // 2. 根据 ActiveLeg 智能推导
                    if (!string.IsNullOrWhiteSpace(context.ActiveLeg))
                    {
                        var leg = context.ActiveLeg.Trim();
                        var resolvedLeg = MatchLegFromEnum(field, leg);
                        if (resolvedLeg.HasValue)
                        {
                            return resolvedLeg.Value;
                        }
                    }

                    if (field.Required)
                    {
                        throw new BusinessException("RCS:MissingRequiredOptionField")
                            .WithData("FieldKey", field.Key)
                            .WithData("Source", "Leg")
                            .WithData("Label", field.Label);
                    }
                    return field.DefaultValue ?? 0;

                case OptionCodeFieldSource.Port:
                    // 1. 优先检查 TaskArgs 中是否有显式传值
                    if (TryGetNumericValue(context.TaskArgs, field.Key, out var explicitPortVal))
                    {
                        return explicitPortVal;
                    }

                    // 2. 尝试从 context.Port 字符串中提取数字
                    if (!string.IsNullOrWhiteSpace(context.Port))
                    {
                        var portVal = ExtractNumber(context.Port);
                        if (portVal.HasValue)
                        {
                            return portVal.Value;
                        }
                    }

                    if (field.Required)
                    {
                        throw new BusinessException("RCS:MissingRequiredOptionField")
                            .WithData("FieldKey", field.Key)
                            .WithData("Source", "Port")
                            .WithData("Label", field.Label);
                    }
                    return field.DefaultValue ?? 0;

                default:
                    return field.DefaultValue ?? 0;
            }
        }

        private static long? MatchLegFromEnum(OptionCodeFieldDefinition field, string leg)
        {
            if (field.Enum == null || field.Enum.Count == 0)
            {
                // 无枚举时按工业通用规范：Fetch(取料)=2, Put(放料)=1
                if (leg.Equals("Fetch", StringComparison.OrdinalIgnoreCase) || leg.Equals("G", StringComparison.OrdinalIgnoreCase))
                {
                    return 2;
                }
                if (leg.Equals("Put", StringComparison.OrdinalIgnoreCase) || leg.Equals("P", StringComparison.OrdinalIgnoreCase))
                {
                    return 1;
                }
                return null;
            }

            var isFetch = leg.Equals("Fetch", StringComparison.OrdinalIgnoreCase) || leg.Contains("取") || leg.Equals("G", StringComparison.OrdinalIgnoreCase);
            var isPut = leg.Equals("Put", StringComparison.OrdinalIgnoreCase) || leg.Contains("放") || leg.Equals("P", StringComparison.OrdinalIgnoreCase);

            foreach (var kvp in field.Enum)
            {
                var desc = kvp.Value;
                if (isFetch && (desc.Contains("Fetch", StringComparison.OrdinalIgnoreCase) || desc.Contains("取") || desc.Contains("(G)")))
                {
                    if (long.TryParse(kvp.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                    {
                        return parsed;
                    }
                }
                else if (isPut && (desc.Contains("Put", StringComparison.OrdinalIgnoreCase) || desc.Contains("放") || desc.Contains("(P)")))
                {
                    if (long.TryParse(kvp.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                    {
                        return parsed;
                    }
                }
            }

            return null;
        }

        private static bool TryGetNumericValue(IReadOnlyDictionary<string, object?>? dict, string key, out long value)
        {
            value = 0;
            if (dict == null)
            {
                return false;
            }

            if (!dict.TryGetValue(key, out var raw) || raw == null)
            {
                return false;
            }

            if (raw is int i) { value = i; return true; }
            if (raw is long l) { value = l; return true; }
            if (raw is uint ui) { value = ui; return true; }
            if (raw is ulong ul) { value = (long)ul; return true; }
            if (raw is short s) { value = s; return true; }
            if (raw is byte b) { value = b; return true; }
            if (raw is JsonElement json)
            {
                if (json.ValueKind == JsonValueKind.Number && json.TryGetInt64(out var jsonNum))
                {
                    value = jsonNum;
                    return true;
                }
                if (json.ValueKind == JsonValueKind.String && long.TryParse(json.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var jsonParsed))
                {
                    value = jsonParsed;
                    return true;
                }
            }

            if (long.TryParse(raw.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var strVal))
            {
                value = strVal;
                return true;
            }

            return false;
        }

        private static long? ExtractNumber(string input)
        {
            var match = DigitsRegex.Match(input);
            if (match.Success && long.TryParse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var num))
            {
                return num;
            }
            return null;
        }
    }
}

