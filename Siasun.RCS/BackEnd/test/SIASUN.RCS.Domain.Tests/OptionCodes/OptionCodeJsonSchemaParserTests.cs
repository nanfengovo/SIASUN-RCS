using System;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// OptionCode JSON Schema 解析与序列化单元测试
    /// </summary>
    public class OptionCodeJsonSchemaParserTests
    {
        private const string SampleJson = @"{
  ""code"": ""sample_site"",
  ""version"": 1,
  ""title"": ""示例现场位图"",
  ""description"": ""测试 Schema 动态反序列化"",
  ""wire"": { ""join"": "","", ""lsbBit1"": true },
  ""parts"": [
    {
      ""key"": ""part1"",
      ""label"": ""第一部分"",
      ""width"": 32,
      ""fields"": [
        {
          ""key"": ""slot"",
          ""label"": ""库位号"",
          ""bitStart"": 1,
          ""bitEnd"": 8,
          ""required"": true,
          ""source"": ""Args"",
          ""enum"": { ""1"": ""A库位"", ""2"": ""B库位"" }
        }
      ]
    }
  ]
}";

        /// <summary>
        /// 验证 JSON 字符串正确反序列化为领域 Schema 结构
        /// </summary>
        [Fact]
        public void Should_Parse_Valid_Json_Schema()
        {
            var schema = OptionCodeJsonSchemaParser.Parse(SampleJson);

            schema.ShouldNotBeNull();
            schema.Code.ShouldBe("sample_site");
            schema.Version.ShouldBe(1);
            schema.Wire.Join.ShouldBe(",");
            schema.Parts.Count.ShouldBe(1);

            var part = schema.Parts[0];
            part.Key.ShouldBe("part1");
            part.Fields.Count.ShouldBe(1);

            var field = part.Fields[0];
            field.Key.ShouldBe("slot");
            field.BitStart.ShouldBe(1);
            field.BitEnd.ShouldBe(8);
            field.BitWidth.ShouldBe(8);
            field.Mask.ShouldBe(255u);
            field.Source.ShouldBe(OptionCodeFieldSource.Args);
            field.Enum.ShouldNotBeNull();
            field.Enum.ShouldContainKey("1");
            field.Enum["1"].ShouldBe("A库位");
        }

        /// <summary>
        /// 验证序列化再反序列化的往返一致性
        /// </summary>
        [Fact]
        public void Should_Support_Serialize_And_Deserialize_Roundtrip()
        {
            var initial = OptionCodeJsonSchemaParser.Parse(SampleJson);
            var serializedJson = OptionCodeJsonSchemaParser.Serialize(initial);
            var roundtrip = OptionCodeJsonSchemaParser.Parse(serializedJson);

            roundtrip.Code.ShouldBe(initial.Code);
            roundtrip.Version.ShouldBe(initial.Version);
            roundtrip.Parts.Count.ShouldBe(initial.Parts.Count);
            roundtrip.Parts[0].Fields[0].Key.ShouldBe("slot");
        }

        /// <summary>
        /// 验证非法 JSON 字符串抛出业务异常
        /// </summary>
        [Fact]
        public void Should_Throw_When_Json_Is_Invalid()
        {
            Should.Throw<ArgumentException>(() => OptionCodeJsonSchemaParser.Parse("   "));
            Should.Throw<BusinessException>(() => OptionCodeJsonSchemaParser.Parse("{ invalid json string }"));
        }
    }
}

