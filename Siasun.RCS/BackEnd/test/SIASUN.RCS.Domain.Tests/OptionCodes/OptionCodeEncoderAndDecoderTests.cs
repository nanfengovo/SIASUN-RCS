using System.Collections.Generic;
using Shouldly;
using Xunit;

namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// OptionCode 正向位掩码编码与逆向反解单元测试（覆盖多现场历史算法精度回归）
    /// </summary>
    public class OptionCodeEncoderAndDecoderTests
    {
        private readonly IOptionCodeEncoder _encoder = new OptionCodeEncoder();
        private readonly IOptionCodeDecoder _decoder = new OptionCodeDecoder();

        /// <summary>
        /// 验证 NXP ERACK 晶圆现场位图编码与历史位移算法输出 100% 精准一致
        /// </summary>
        [Fact]
        public void Should_Encode_And_Decode_Erack_Accurately()
        {
            var schema = TestOptionCodeSchemas.CreateErackV1();

            // 测试用例参数
            var cameraTemplateIndex = 5u;
            var agvLocationIndex = 1u;
            var boxType = 3u;
            var machineIndex = 12u;

            var deviceType = 2u; // 对应 Stocker
            var machineLocationIndex = 8u;
            var pGMark = 1u; // 对应 Fetch(取料)
            var machineNumber = 42u;

            // 历史硬编码位移基准计算公式
            var legacyCode1 = ((machineIndex & 0xFFF) << 20)
                            | ((boxType & 0xF) << 16)
                            | ((agvLocationIndex & 0xFF) << 8)
                            | (cameraTemplateIndex & 0xFF);

            var legacyCode2 = ((machineNumber & 0xFFF) << 20)
                            | ((pGMark & 0xF) << 16)
                            | ((machineLocationIndex & 0xFF) << 8)
                            | (deviceType & 0xFF);

            var expectedStr = $"{legacyCode1},{legacyCode2}";

            // Schema 驱动编码
            var assembled = new Dictionary<string, Dictionary<string, uint>>
            {
                ["TaskCode1"] = new()
                {
                    ["cameraTemplateIndex"] = cameraTemplateIndex,
                    ["agvLocationIndex"] = agvLocationIndex,
                    ["boxType"] = boxType,
                    ["machineIndex"] = machineIndex
                },
                ["TaskCode2"] = new()
                {
                    ["deviceType"] = deviceType,
                    ["machineLocationIndex"] = machineLocationIndex,
                    ["pGMark"] = pGMark,
                    ["machineNumber"] = machineNumber
                }
            };

            var encoded = _encoder.Encode(schema, assembled);
            encoded.ShouldBe(expectedStr);

            // 反向逆向反解
            var decoded = _decoder.Decode(schema, encoded);
            decoded.Parts.Count.ShouldBe(2);

            var part1 = decoded.Parts[0];
            part1.RawWord.ShouldBe(legacyCode1);

            var part2 = decoded.Parts[1];
            part2.RawWord.ShouldBe(legacyCode2);

            // 校验枚举语义反查
            var pGMarkField = part2.Fields.Find(f => f.FieldKey == "pGMark");
            pGMarkField.ShouldNotBeNull();
            pGMarkField.RawValue.ShouldBe(1u);
            pGMarkField.DisplayValue.ShouldBe("Fetch(取料)");

            var deviceTypeField = part2.Fields.Find(f => f.FieldKey == "deviceType");
            deviceTypeField.ShouldNotBeNull();
            deviceTypeField.RawValue.ShouldBe(2u);
            deviceTypeField.DisplayValue.ShouldBe("Stocker");
        }

        /// <summary>
        /// 验证 NXP 天津 Molding 车间现场标准用例输出为 "33621253,33687299"
        /// </summary>
        [Fact]
        public void Should_Encode_And_Decode_Molding_With_Exact_Historical_Values()
        {
            var schema = TestOptionCodeSchemas.CreateMoldingV1();

            // Molding 现场测试例:
            // TaskCode1 (0x02010505 = 33621253): count=2, carrierType=1(小弹匣L), ttStart=5, lotId=5
            // TaskCode2 (0x02020703 = 33687299): count=2, putOrFetchFlag=2(Fetch), machineLocationId=7, machineType=3(SP170)
            var assembled = new Dictionary<string, Dictionary<string, uint>>
            {
                ["TaskCode1"] = new()
                {
                    ["lotId"] = 5u,
                    ["ttStart"] = 5u,
                    ["carrierType"] = 1u,
                    ["count"] = 2u
                },
                ["TaskCode2"] = new()
                {
                    ["machineType"] = 3u,
                    ["machineLocationId"] = 7u,
                    ["putOrFetchFlag"] = 2u,
                    ["count"] = 2u
                }
            };

            var encoded = _encoder.Encode(schema, assembled);
            encoded.ShouldBe("33621253,33687299");

            // 逆向反解
            var decoded = _decoder.Decode(schema, "33621253,33687299");
            decoded.Parts.Count.ShouldBe(2);

            // 验证 16 进制字
            decoded.Parts[0].HexWord.ShouldBe("0x02010505");
            decoded.Parts[1].HexWord.ShouldBe("0x02020703");

            // 验证字段业务语义
            var carrierField = decoded.Parts[0].Fields.Find(f => f.FieldKey == "carrierType");
            carrierField.ShouldNotBeNull();
            carrierField.RawValue.ShouldBe(1u);
            carrierField.DisplayValue.ShouldBe("小弹匣(L)");

            var machineTypeField = decoded.Parts[1].Fields.Find(f => f.FieldKey == "machineType");
            machineTypeField.ShouldNotBeNull();
            machineTypeField.RawValue.ShouldBe(3u);
            machineTypeField.DisplayValue.ShouldBe("SP170");

            var flagField = decoded.Parts[1].Fields.Find(f => f.FieldKey == "putOrFetchFlag");
            flagField.ShouldNotBeNull();
            flagField.RawValue.ShouldBe(2u);
            flagField.DisplayValue.ShouldBe("Fetch(取料)");
        }

        /// <summary>
        /// 验证台湾晶技 TXC 模式编解码与往返（Roundtrip）一致性
        /// </summary>
        [Fact]
        public void Should_Support_Roundtrip_Encoding_And_Decoding_For_Txc()
        {
            var schema = TestOptionCodeSchemas.CreateTxcDemoV1();

            var assembled = new Dictionary<string, Dictionary<string, uint>>
            {
                ["codeA"] = new()
                {
                    ["armSide"] = 2u,       // 右侧
                    ["agvSlot"] = 0u,       // 常量
                    ["boxType"] = 8u,
                    ["machineIndex"] = 15u
                },
                ["codeB"] = new()
                {
                    ["equipmentType"] = 1u, // Rack货架
                    ["equipmentSlot"] = 4u,
                    ["pickPlace"] = 1u,     // 车身到设备(P/Put)
                    ["machineNo"] = 20u
                }
            };

            var encoded = _encoder.Encode(schema, assembled);
            var decoded = _decoder.Decode(schema, encoded);

            // 验证反解出来的数值与组装数值 100% 对应
            foreach (var part in decoded.Parts)
            {
                assembled.ShouldContainKey(part.PartKey);
                foreach (var field in part.Fields)
                {
                    assembled[part.PartKey][field.FieldKey].ShouldBe(field.RawValue);
                }
            }
        }
    }
}

