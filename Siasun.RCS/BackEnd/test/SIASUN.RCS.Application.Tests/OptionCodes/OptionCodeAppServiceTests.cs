using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.OptionCodes.Dtos;
using SIASUN.RCS.Tasks;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// OptionCode 应用服务单元测试
    /// 验证 Schema 检索、动态参数编译、反向解码与在途任务快照冻结
    /// </summary>
    public class OptionCodeAppServiceTests
    {
        private readonly IOptionCodeSchemaRegistry _schemaRegistry;
        private readonly IOptionCodeAssembler _assembler;
        private readonly IOptionCodeEncoder _encoder;
        private readonly IOptionCodeDecoder _decoder;
        private readonly IRepository<AgvTask, Guid> _taskRepo;
        private readonly OptionCodeAppService _appService;

        /// <summary>
        /// 构造函数，初始化服务依赖
        /// </summary>
        public OptionCodeAppServiceTests()
        {
            _schemaRegistry = new OptionCodeSchemaRegistry();
            _schemaRegistry = new OptionCodeSchemaRegistry(TestOptionCodeSchemas.GetDefaultTestSchemas());
            _assembler = new OptionCodeAssembler();
            _encoder = new OptionCodeEncoder();
            _decoder = new OptionCodeDecoder();
            _taskRepo = Substitute.For<IRepository<AgvTask, Guid>>();

            _appService = new OptionCodeAppService(
                _schemaRegistry,
                _assembler,
                _encoder,
                _decoder,
                _taskRepo);
        }

        /// <summary>
        /// 验证获取已注册 Schema 列表与指定 Schema 详情
        /// </summary>
        [Fact]
        public async Task Should_Get_Schemas_And_Detail()
        {
            var schemas = await _appService.GetSchemasAsync();
            schemas.Count.ShouldBeGreaterThanOrEqualTo(3);

            var txc = await _appService.GetSchemaAsync("txc_demo", 1);
            txc.ShouldNotBeNull();
            txc.Code.ShouldBe("txc_demo");
            txc.Parts.Count.ShouldBe(2);
        }

        /// <summary>
        /// 验证动态编译接口能够正确输出 OptionCode 串与即时反解预览画像
        /// </summary>
        [Fact]
        public async Task Should_Compile_OptionCode_With_Instant_Decoded_Preview()
        {
            var input = new CompileOptionCodeInput
            {
                SchemaCode = "molding",
                SchemaVersion = 1,
                TaskArgs = new Dictionary<string, object?>
                {
                    { "lotId", 5 },
                    { "ttStart", 5 },
                    { "carrierType", 1 }, // 小弹匣(L)
                    { "count", 2 }
                },
                MasterValues = new Dictionary<string, object?>
                {
                    { "machineType", 3 } // SP170
                },
                ActiveLeg = "Fetch", // putOrFetchFlag = 2
                Port = "7"           // machineLocationId = 7
            };

            var result = await _appService.CompileAsync(input);

            result.ShouldNotBeNull();
            result.OptionCode.ShouldBe("33621253,33687299");
            result.Words.Count.ShouldBe(2);
            result.Words[0].ShouldBe(33621253u);
            result.Words[1].ShouldBe(33687299u);

            // 即时反解画像验证
            result.DecodedDetail.ShouldNotBeNull();
            result.DecodedDetail.Parts.Count.ShouldBe(2);
            result.DecodedDetail.Parts[0].HexWord.ShouldBe("0x02010505");
            result.DecodedDetail.Parts[1].HexWord.ShouldBe("0x02020703");
        }

        /// <summary>
        /// 验证反向解码接口能够将 32 位位图拆解为人类可读的业务语义
        /// </summary>
        [Fact]
        public async Task Should_Decode_OptionCode_String_Accurately()
        {
            var input = new DecodeOptionCodeInput
            {
                SchemaCode = "molding",
                SchemaVersion = 1,
                OptionCode = "33621253,33687299"
            };

            var result = await _appService.DecodeAsync(input);

            result.ShouldNotBeNull();
            result.Parts.Count.ShouldBe(2);

            var part1 = result.Parts[0];
            var carrier = part1.Fields.Find(f => f.FieldKey == "carrierType");
            carrier.ShouldNotBeNull();
            carrier.DisplayValue.ShouldBe("小弹匣(L)");

            var part2 = result.Parts[1];
            var machine = part2.Fields.Find(f => f.FieldKey == "machineType");
            machine.ShouldNotBeNull();
            machine.DisplayValue.ShouldBe("SP170");

            var flag = part2.Fields.Find(f => f.FieldKey == "putOrFetchFlag");
            flag.ShouldNotBeNull();
            flag.DisplayValue.ShouldBe("Fetch(取料)");
        }

        /// <summary>
        /// 验证为指定在途运单冻结并固化 OptionCode 快照
        /// </summary>
        [Fact]
        public async Task Should_Freeze_Task_OptionCode_Successfully()
        {
            var taskId = Guid.NewGuid();
            var task = new AgvTask(taskId, "TASK-FREEZE-01");

            _taskRepo.GetAsync(taskId).Returns(Task.FromResult(task));

            var input = new FreezeTaskOptionCodeInput
            {
                TaskId = taskId,
                SchemaCode = "molding",
                SchemaVersion = 1,
                ExplicitOptionCode = "33621253,33687299"
            };

            var result = await _appService.FreezeTaskOptionCodeAsync(input);

            result.OptionCode.ShouldBe("33621253,33687299");
            task.OptionCode.ShouldBe("33621253,33687299");
            task.OptionCodeSchemaCode.ShouldBe("molding");
            task.OptionCodeSchemaVersion.ShouldBe(1);

            await _taskRepo.Received(1).UpdateAsync(task, autoSave: true);
        }
    }
}

