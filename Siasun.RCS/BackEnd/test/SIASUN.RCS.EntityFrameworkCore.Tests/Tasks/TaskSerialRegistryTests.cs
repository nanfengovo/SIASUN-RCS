using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using SIASUN.RCS.EntityFrameworkCore;
using SIASUN.RCS.Tasks;
using Volo.Abp.Guids;
using Xunit;

namespace SIASUN.RCS.EntityFrameworkCore.Tasks
{
    /// <summary>
    /// TM 底层序列号注册中心与物理表持久化测试（严格落地选项 B：跨进程崩溃自愈与消除字符串 hack）
    /// </summary>
    [Collection(RCSTestConsts.CollectionDefinitionName)]
    public class TaskSerialRegistryTests : RCSEntityFrameworkCoreTestBase
    {
        private readonly ITaskSerialRegistry _serialRegistry;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IGuidGenerator _guidGenerator;
        private readonly ILogger<TaskSerialRegistry> _logger;

        public TaskSerialRegistryTests()
        {
            _serialRegistry = GetRequiredService<ITaskSerialRegistry>();
            _scopeFactory = GetRequiredService<IServiceScopeFactory>();
            _guidGenerator = GetRequiredService<IGuidGenerator>();
            _logger = GetRequiredService<ILogger<TaskSerialRegistry>>();
        }

        [Fact]
        public async Task Should_Register_And_Retrieve_By_TmSerial()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var taskCode = "TSK-TEST-001";
            var tmSerial = "TM_SERIAL_20260908_001";
            var leg = "Fetch";
            var stepIndex = 1;
            var waitingEvent = "TM_FETCH_DONE";
            var vehicleCode = "AGV-01";

            // Act: 注册
            var mapping = await _serialRegistry.RegisterAsync(
                taskId, taskCode, tmSerial, leg, stepIndex, waitingEvent, vehicleCode);

            // Assert: 即时检索
            mapping.ShouldNotBeNull();
            mapping.TmSerial.ShouldBe(tmSerial);
            mapping.TaskId.ShouldBe(taskId);
            mapping.TaskCode.ShouldBe(taskCode);
            mapping.Leg.ShouldBe(leg);
            mapping.StepIndex.ShouldBe(stepIndex);
            mapping.WaitingEvent.ShouldBe(waitingEvent);
            mapping.VehicleCode.ShouldBe(vehicleCode);

            // 通过 TM 序列号反查内部任务
            var retrieved = await _serialRegistry.FindByTmSerialAsync(tmSerial);
            retrieved.ShouldNotBeNull();
            retrieved.TaskId.ShouldBe(taskId);
            retrieved.TaskCode.ShouldBe(taskCode);
            retrieved.Leg.ShouldBe(leg);
            retrieved.WaitingEvent.ShouldBe("TM_FETCH_DONE");
        }

        [Fact]
        public async Task Should_Recover_From_Database_When_Cache_Is_Cleared()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var taskCode = "TSK-TEST-RECOVER";
            var tmSerial = "TM_RECOVER_999";

            await _serialRegistry.RegisterAsync(
                taskId, taskCode, tmSerial, "Put", 4, "TM_PUT_DONE", "AGV-02");

            // 模拟工控机断电/服务重启：新建一个空的 TaskSerialRegistry 实例（无内存缓存）
            var freshRegistry = new TaskSerialRegistry(_scopeFactory, _guidGenerator, _logger);

            // Act: 从空内存实例中按 TM 序列号反查
            var recovered = await freshRegistry.FindByTmSerialAsync(tmSerial);

            // Assert: 验证必须能从 EF Core 物理表 AppTaskSerialMappings 自动恢复
            recovered.ShouldNotBeNull();
            recovered.TaskId.ShouldBe(taskId);
            recovered.TaskCode.ShouldBe(taskCode);
            recovered.Leg.ShouldBe("Put");
            recovered.StepIndex.ShouldBe(4);
            recovered.WaitingEvent.ShouldBe("TM_PUT_DONE");
        }

        [Fact]
        public async Task Should_Support_Multi_Leg_Per_Task_And_Remove_When_Complete()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var taskCode = "TSK-MULTI-LEG";
            var tmFetch = "TM_LEG_FETCH_" + Guid.NewGuid().ToString("N")[..8];
            var tmPut = "TM_LEG_PUT_" + Guid.NewGuid().ToString("N")[..8];

            // Act 1: 注册 Fetch 程段与 Put 程段
            await _serialRegistry.RegisterAsync(taskId, taskCode, tmFetch, "Fetch", 1, "TM_FETCH_DONE");
            await _serialRegistry.RegisterAsync(taskId, taskCode, tmPut, "Put", 4, "TM_PUT_DONE");

            // Assert 1: 按 TaskId 能查出该任务绑定的全部多程段流水号
            var legs = await _serialRegistry.GetByTaskIdAsync(taskId);
            legs.Count.ShouldBe(2);
            legs.Any(x => x.TmSerial == tmFetch && x.Leg == "Fetch").ShouldBeTrue();
            legs.Any(x => x.TmSerial == tmPut && x.Leg == "Put").ShouldBeTrue();

            // Act 2: 任务完结后清理
            await _serialRegistry.RemoveByTaskIdAsync(taskId);

            // Assert 2: 清理后无法反查
            var afterFetch = await _serialRegistry.FindByTmSerialAsync(tmFetch);
            var afterPut = await _serialRegistry.FindByTmSerialAsync(tmPut);
            afterFetch.ShouldBeNull();
            afterPut.ShouldBeNull();
        }
    }
}
