using System;
using System.Collections.Generic;
using Volo.Abp;

namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// OptionCode 32 位位掩码正向编译器默认实现
    /// </summary>
    public class OptionCodeEncoder : IOptionCodeEncoder
    {
        /// <inheritdoc />
        public IReadOnlyList<uint> EncodeToWords(
            OptionCodeSchemaDefinition schema,
            Dictionary<string, Dictionary<string, uint>> assembledValues)
        {
            Check.NotNull(schema, nameof(schema));
            Check.NotNull(assembledValues, nameof(assembledValues));

            var words = new List<uint>();

            foreach (var part in schema.Parts)
            {
                uint word = 0;

                if (assembledValues.TryGetValue(part.Key, out var fieldValues))
                {
                    foreach (var field in part.Fields)
                    {
                        if (fieldValues.TryGetValue(field.Key, out var rawVal))
                        {
                            // 掩码过滤并左移
                            var masked = rawVal & field.Mask;
                            word |= masked << field.Shift;
                        }
                    }
                }

                words.Add(word);
            }

            return words;
        }

        /// <inheritdoc />
        public string Encode(
            OptionCodeSchemaDefinition schema,
            Dictionary<string, Dictionary<string, uint>> assembledValues)
        {
            var words = EncodeToWords(schema, assembledValues);
            var joinDelimiter = schema.Wire?.Join ?? ",";
            return string.Join(joinDelimiter, words);
        }
    }
}

