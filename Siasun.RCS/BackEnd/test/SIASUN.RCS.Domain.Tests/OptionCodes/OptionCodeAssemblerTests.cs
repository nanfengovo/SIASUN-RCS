using System.Collections.Generic;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// OptionCode 字段多源装配器单元测试
    /// </summary>
    public class OptionCodeAssemblerTests
    {
        private readonly IOptionCodeAssembler _assembler = new OptionCodeAssembler();

        /// <summary>
        /// 验证从 Const、Args、Master、Leg、Port 各数据源正确解析数值
        /// </summary>
        [Fact]
        public void Should_Assemble_Values_From_Multiple_Sources_Successfully()
        {
            var schema = TestOptionCodeSchemas.CreateTxcDemoV1();

            var taskArgs = new Dictionary<string, object?>
            {
                { "boxType", 5 },
                { "machineIndex", 12 }
            };

            var masterValues = new Dictionary<string, object?>
            {
                { "armSide", 2 },
                { "equipmentType", 3 },
                { "machineNo", 99 }
            };

            var context = new OptionCodeAssembleContext
            {
                TaskArgs = taskArgs,
                MasterValues = masterValues,
                ActiveLeg = "Fetch", // 对应 pickPlace: 设备到车身(G/Fetch) = 2
                Port = "Slot-07"     // 提取 7
            };

            var assembled = _assembler.Assemble(schema, context);

            assembled.ShouldContainKey("codeA");
            assembled.ShouldContainKey("codeB");

            // codeA: armSide=2, agvSlot=0 (const), boxType=5, machineIndex=12
            assembled["codeA"]["armSide"].ShouldBe(2u);
            assembled["codeA"]["agvSlot"].ShouldBe(0u);
            assembled["codeA"]["boxType"].ShouldBe(5u);
            assembled["codeA"]["machineIndex"].ShouldBe(12u);

            // codeB: equipmentType=3, equipmentSlot=7, pickPlace=2, machineNo=99
            assembled["codeB"]["equipmentType"].ShouldBe(3u);
            assembled["codeB"]["equipmentSlot"].ShouldBe(7u);
            assembled["codeB"]["pickPlace"].ShouldBe(2u);
            assembled["codeB"]["machineNo"].ShouldBe(99u);
        }

        /// <summary>
        /// 验证当必需字段缺失时抛出业务异常
        /// </summary>
        [Fact]
        public void Should_Throw_When_Required_Field_Is_Missing()
        {
            var schema = TestOptionCodeSchemas.CreateTxcDemoV1();

            // 缺少 equipmentType (required=true, source=Master)
            var context = new OptionCodeAssembleContext
            {
                ActiveLeg = "Fetch",
                Port = "1"
            };

            var ex = Should.Throw<BusinessException>(() => _assembler.Assemble(schema, context));
            ex.Code.ShouldBe("RCS:MissingRequiredOptionField");
            ex.Data["FieldKey"].ShouldBe("equipmentType");
        }

        /// <summary>
        /// 验证当字段数值超出定义的位宽上限时抛出溢出拦截异常
        /// </summary>
        [Fact]
        public void Should_Throw_When_Field_Value_Overflows_BitWidth()
        {
            var schema = TestOptionCodeSchemas.CreateTxcDemoV1();

            // boxType 位宽为 8Bit (最大 255)，传入 300
            var taskArgs = new Dictionary<string, object?>
            {
                { "boxType", 300 }
            };

            var masterValues = new Dictionary<string, object?>
            {
                { "equipmentType", 1 }
            };

            var context = new OptionCodeAssembleContext
            {
                TaskArgs = taskArgs,
                MasterValues = masterValues,
                ActiveLeg = "Put",
                Port = "1"
            };

            var ex = Should.Throw<BusinessException>(() => _assembler.Assemble(schema, context));
            ex.Code.ShouldBe("RCS:OptionFieldValueOverflow");
            ex.Data["FieldKey"].ShouldBe("boxType");
        }
    }
}
