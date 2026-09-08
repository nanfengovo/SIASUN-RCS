using System;
using System.Collections.Generic;
using System.Globalization;
using Volo.Abp;

namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// OptionCode 32 位逆向拆解与语义反查器默认实现
    /// </summary>
    public class OptionCodeDecoder : IOptionCodeDecoder
    {
        /// <inheritdoc />
        public DecodedOptionCodeResult DecodeWords(
            OptionCodeSchemaDefinition schema,
            IReadOnlyList<uint> words)
        {
            Check.NotNull(schema, nameof(schema));
            Check.NotNull(words, nameof(words));

            var delimiter = schema.Wire?.Join ?? ",";
            var optionCodeStr = string.Join(delimiter, words);

            var result = new DecodedOptionCodeResult
            {
                SchemaCode = schema.Code,
                SchemaVersion = schema.Version,
                SchemaTitle = schema.Title,
                OptionCode = optionCodeStr
            };

            for (var i = 0; i < schema.Parts.Count; i++)
            {
                var partDef = schema.Parts[i];
                var rawWord = i < words.Count ? words[i] : 0u;

                var partResult = new DecodedPartResult
                {
                    PartKey = partDef.Key,
                    Label = partDef.Label,
                    Width = partDef.Width,
                    RawWord = rawWord
                };

                foreach (var field in partDef.Fields)
                {
                    var rawVal = (rawWord >> field.Shift) & field.Mask;
                    var displayVal = rawVal.ToString();

                    if (field.Enum != null && field.Enum.TryGetValue(rawVal.ToString(), out var enumLabel))
                    {
                        displayVal = enumLabel;
                    }

                    var binaryStr = Convert.ToString(rawVal, 2).PadLeft(field.BitWidth, '0');

                    partResult.Fields.Add(new DecodedFieldResult
                    {
                        FieldKey = field.Key,
                        Label = field.Label,
                        BitStart = field.BitStart,
                        BitEnd = field.BitEnd,
                        BitWidth = field.BitWidth,
                        RawValue = rawVal,
                        DisplayValue = displayVal,
                        BinaryString = binaryStr,
                        Source = field.Source,
                        Description = field.Description
                    });
                }

                result.Parts.Add(partResult);
            }

            return result;
        }

        /// <inheritdoc />
        public DecodedOptionCodeResult Decode(
            OptionCodeSchemaDefinition schema,
            string optionCodeString)
        {
            Check.NotNull(schema, nameof(schema));
            Check.NotNullOrWhiteSpace(optionCodeString, nameof(optionCodeString));

            var delimiter = schema.Wire?.Join ?? ",";
            var segments = optionCodeString.Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries);
            var words = new List<uint>();

            foreach (var seg in segments)
            {
                var trimmed = seg.Trim();
                if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    words.Add(Convert.ToUInt32(trimmed.Substring(2), 16));
                }
                else if (uint.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var uVal))
                {
                    words.Add(uVal);
                }
                else if (long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var lVal))
                {
                    words.Add((uint)lVal);
                }
                else
                {
                    words.Add(0u);
                }
            }

            var result = DecodeWords(schema, words);
            result.OptionCode = optionCodeString;
            return result;
        }
    }
}

