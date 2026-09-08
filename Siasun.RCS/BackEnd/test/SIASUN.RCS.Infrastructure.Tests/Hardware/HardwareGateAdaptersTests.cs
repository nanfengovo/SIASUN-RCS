using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using SIASUN.RCS.Adapters.Hardware;
using SIASUN.RCS.Hardware;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Tests.Hardware
{
    /// <summary>
    /// 04.Adapters 工业现场硬件适配器插件单元测试
    /// 验证洁净室风淋门、双臂防撞互锁与仿真 Mock 适配器行为
    /// </summary>
    public class HardwareGateAdaptersTests
    {
        /// <summary>
        /// 测试 Mock 硬件网关适配器正常模式与故障模拟模式
        /// </summary>
        [Fact]
        public async Task MockHardwareGateAdapter_Should_Support_Simulation_Modes()
        {
            var adapter = new MockHardwareGateAdapter(NullLogger<MockHardwareGateAdapter>.Instance);
            var context = new HardwareGateContext("DEVICE_MOCK_01", locationCode: "LOC_01");

            // 1. 正常通过
            var condResult = await adapter.CheckConditionAsync(context);
            condResult.IsSuccess.ShouldBeTrue();

            var actResult = await adapter.ExecuteActionAsync(context, "OpenDoor");
            actResult.IsSuccess.ShouldBeTrue();

            var signalArrived = await adapter.WaitForSignalAsync(context, "Ready", TimeSpan.FromSeconds(1));
            signalArrived.ShouldBeTrue();

            // 2. 模拟条件校验失败
            adapter.SimulateConditionFailure = true;
            var condFailResult = await adapter.CheckConditionAsync(context);
            condFailResult.IsSuccess.ShouldBeFalse();
            condFailResult.Code.ShouldBe("MOCK_CONDITION_BLOCKED");

            // 3. 模拟动作执行失败
            adapter.SimulateActionFailure = true;
            var actFailResult = await adapter.ExecuteActionAsync(context, "OpenDoor");
            actFailResult.IsSuccess.ShouldBeFalse();
            actFailResult.Code.ShouldBe("MOCK_ACTION_FAILED");
        }

        /// <summary>
        /// 测试洁净室风淋门适配器在互锁闭锁与动作执行时的逻辑
        /// </summary>
        [Fact]
        public async Task CleanroomAirShowerGateAdapter_Should_Handle_Interlock_And_Actions()
        {
            var adapter = new CleanroomAirShowerGateAdapter(NullLogger<CleanroomAirShowerGateAdapter>.Instance);

            // 1. 正常状态检查
            var readyContext = new HardwareGateContext("AIR_SHOWER_01", vehicleCode: "AGV_01", taskCode: "TASK_001");
            var readyResult = await adapter.CheckConditionAsync(readyContext);
            readyResult.IsSuccess.ShouldBeTrue();
            readyResult.Message.ShouldNotBeNull();
            readyResult.Message.ShouldContain("允许通行申请");

            // 2. 互锁激活冲突检查
            var blockedContext = new HardwareGateContext("AIR_SHOWER_01", vehicleCode: "AGV_01")
            {
                Parameters = new Dictionary<string, string> { ["InterlockBlocked"] = "true" }
            };
            var blockedResult = await adapter.CheckConditionAsync(blockedContext);
            blockedResult.IsSuccess.ShouldBeFalse();
            blockedResult.Code.ShouldBe("AIR_SHOWER_INTERLOCK_ACTIVE");

            // 3. 动作执行
            var openResult = await adapter.ExecuteActionAsync(readyContext, "OpenDoor");
            openResult.IsSuccess.ShouldBeTrue();

            var closeResult = await adapter.ExecuteActionAsync(readyContext, "CloseDoor");
            closeResult.IsSuccess.ShouldBeTrue();

            var releaseResult = await adapter.ExecuteActionAsync(readyContext, "ReleaseInterlock");
            releaseResult.IsSuccess.ShouldBeTrue();

            // 4. 不支持的非法动作
            var invalidResult = await adapter.ExecuteActionAsync(readyContext, "InvalidAction");
            invalidResult.IsSuccess.ShouldBeFalse();
            invalidResult.Code.ShouldBe("UNSUPPORTED_DOOR_ACTION");
        }

        /// <summary>
        /// 测试半导体晶圆 AGV 双臂协同防撞与光电净空校验
        /// </summary>
        [Fact]
        public async Task TwinArmInterlockGateAdapter_Should_Prevent_Collision_And_Control_Sync()
        {
            var adapter = new TwinArmInterlockGateAdapter(NullLogger<TwinArmInterlockGateAdapter>.Instance);

            // 1. 正常状态检查
            var safeContext = new HardwareGateContext("AGV_ARM_GATE", vehicleCode: "AGV_WAFER_01", locationCode: "STK_PORT_01");
            var safeResult = await adapter.CheckConditionAsync(safeContext);
            safeResult.IsSuccess.ShouldBeTrue();

            // 2. 干涉冲突检查
            var hazardContext = new HardwareGateContext("AGV_ARM_GATE", vehicleCode: "AGV_WAFER_01")
            {
                Parameters = new Dictionary<string, string> { ["TwinArmConflict"] = "true" }
            };
            var hazardResult = await adapter.CheckConditionAsync(hazardContext);
            hazardResult.IsSuccess.ShouldBeFalse();
            hazardResult.Code.ShouldBe("TWIN_ARM_COLLISION_HAZARD");

            // 3. 动作控制
            var lockResult = await adapter.ExecuteActionAsync(safeContext, "LockTwinArmSync");
            lockResult.IsSuccess.ShouldBeTrue();

            var checkResult = await adapter.ExecuteActionAsync(safeContext, "CheckClearance");
            checkResult.IsSuccess.ShouldBeTrue();

            var unlockResult = await adapter.ExecuteActionAsync(safeContext, "UnlockTwinArmSync");
            unlockResult.IsSuccess.ShouldBeTrue();
        }
    }
}
