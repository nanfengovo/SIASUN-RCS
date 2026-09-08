using System.Linq;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// OptionCode Schema 统一注册中心单元测试
    /// </summary>
    public class OptionCodeSchemaRegistryTests
    {
        private readonly IOptionCodeSchemaRegistry _registry = new OptionCodeSchemaRegistry(TestOptionCodeSchemas.GetDefaultTestSchemas());

        /// <summary>
        /// 验证内置的行业现场三大 Schema 均已默认加载
        /// </summary>
        [Fact]
        public void Should_Contain_BuiltIn_Schemas_On_Initialization()
        {
            var schemas = _registry.GetAll();

            schemas.Count.ShouldBeGreaterThanOrEqualTo(3);
            schemas.Any(s => s.Code == "txc_demo").ShouldBeTrue();
            schemas.Any(s => s.Code == "erack").ShouldBeTrue();
            schemas.Any(s => s.Code == "molding").ShouldBeTrue();
        }

        /// <summary>
        /// 验证按 code 或 code+version 查询 Schema 的行为
        /// </summary>
        [Fact]
        public void Should_Find_Schema_By_Code_And_Version()
        {
            var txcSchema = _registry.Find("txc_demo");
            txcSchema.ShouldNotBeNull();
            txcSchema.Version.ShouldBe(1);

            var erackSchema = _registry.Get("erack.v1");
            erackSchema.ShouldNotBeNull();
            erackSchema.Code.ShouldBe("erack");
            erackSchema.Version.ShouldBe(1);

            var notFound = _registry.Find("non_existent");
            notFound.ShouldBeNull();

            Should.Throw<BusinessException>(() => _registry.Get("non_existent"));
        }

        /// <summary>
        /// 验证动态注册自定义 Schema 并在缺省版本时返回最新最高版本
        /// </summary>
        [Fact]
        public void Should_Register_And_Resolve_Latest_Version()
        {
            var v1 = new OptionCodeSchemaDefinition("custom_site", 1, "Custom Site V1");
            var v2 = new OptionCodeSchemaDefinition("custom_site", 2, "Custom Site V2");

            _registry.Register(v1);
            _registry.Register(v2);

            var latest = _registry.Get("custom_site");
            latest.Version.ShouldBe(2);

            var specific = _registry.Get("custom_site", 1);
            specific.Version.ShouldBe(1);
        }
    }
}

