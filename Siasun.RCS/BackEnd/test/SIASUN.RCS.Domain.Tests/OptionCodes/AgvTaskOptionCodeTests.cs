using System;
using Shouldly;
using SIASUN.RCS.Tasks;
using Xunit;

namespace SIASUN.RCS.OptionCodes
{
    /// <summary>
    /// AgvTask 任务聚合根 OptionCode 快照冻结领域行为单元测试
    /// </summary>
    public class AgvTaskOptionCodeTests
    {
        /// <summary>
        /// 验证任务建单时支持传入 OptionCode 及其关联 Schema 信息
        /// </summary>
        [Fact]
        public void Should_Create_Task_With_OptionCode_And_Schema_Metadata()
        {
            var id = Guid.NewGuid();
            var task = new AgvTask(
                id,
                taskCode: "TASK-OPT-001",
                fromStation: "ST-01",
                toStation: "ST-02",
                carrierCode: "CARRIER-88",
                batchId: "BATCH-2026",
                optionCode: "0,65536",
                traceId: "TRACE-999",
                optionCodeSchemaCode: "erack",
                optionCodeSchemaVersion: 1);

            task.OptionCode.ShouldBe("0,65536");
            task.OptionCodeSchemaCode.ShouldBe("erack");
            task.OptionCodeSchemaVersion.ShouldBe(1);
        }

        /// <summary>
        /// 验证 FreezeOptionCode 领域行为成功固化 OptionCode 与 Schema 版本快照
        /// </summary>
        [Fact]
        public void Should_Freeze_OptionCode_Snapshot_Successfully()
        {
            var id = Guid.NewGuid();
            var task = new AgvTask(id, "TASK-OPT-002");

            task.OptionCode.ShouldBeNull();
            task.OptionCodeSchemaCode.ShouldBeNull();
            task.OptionCodeSchemaVersion.ShouldBeNull();

            task.FreezeOptionCode("33621253,33687299", "molding", 1);

            task.OptionCode.ShouldBe("33621253,33687299");
            task.OptionCodeSchemaCode.ShouldBe("molding");
            task.OptionCodeSchemaVersion.ShouldBe(1);
        }
    }
}

