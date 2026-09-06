using System;
using Shouldly;
using SIASUN.RCS.Vehicles;
using Volo.Abp;
using Xunit;

namespace SIASUN.RCS.Domain.Tests.Vehicles
{
    /// <summary>
    /// AGV 车辆领域模型单元测试
    /// 验证车辆状态迁移、任务指派、遥测数据更新以及人工复位逻辑
    /// </summary>
    public class AgvVehicleTests
    {
        [Fact]
        public void Constructor_Should_Initialize_With_Idle_Status()
        {
            var id = Guid.NewGuid();
            var vehicle = new AgvVehicle(id, "AGV-TEST-01", "192.168.1.50", "STATION-HOME");

            vehicle.Id.ShouldBe(id);
            vehicle.VehicleCode.ShouldBe("AGV-TEST-01");
            vehicle.Status.ShouldBe(VehicleStatus.Idle);
            vehicle.IpAddress.ShouldBe("192.168.1.50");
            vehicle.CurrentStation.ShouldBe("STATION-HOME");
            vehicle.BatteryPercentage.ShouldBe(100.0);
            vehicle.CurrentTaskId.ShouldBeNull();
            vehicle.CurrentTaskCode.ShouldBeNull();
            vehicle.ErrorMessage.ShouldBeNull();
        }

        [Fact]
        public void AssignTask_Should_Bind_Task_And_Set_Running()
        {
            var vehicle = new AgvVehicle(Guid.NewGuid(), "AGV-01");
            var taskId = Guid.NewGuid();

            vehicle.AssignTask(taskId, "TASK-2026-X");

            vehicle.Status.ShouldBe(VehicleStatus.Running);
            vehicle.CurrentTaskId.ShouldBe(taskId);
            vehicle.CurrentTaskCode.ShouldBe("TASK-2026-X");
        }

        [Fact]
        public void AssignTask_When_Error_Should_Throw_BusinessException()
        {
            var vehicle = new AgvVehicle(Guid.NewGuid(), "AGV-ERR-01");
            vehicle.ReportError("机械臂抱死");

            var ex = Should.Throw<BusinessException>(() =>
            {
                vehicle.AssignTask(Guid.NewGuid(), "TASK-REJECT");
            });

            ex.Code.ShouldBe("RCS:VehicleNotAvailableForTask");
        }

        [Fact]
        public void ReleaseTask_Should_Reset_To_Idle_When_Not_In_Error()
        {
            var vehicle = new AgvVehicle(Guid.NewGuid(), "AGV-RELEASE-01");
            vehicle.AssignTask(Guid.NewGuid(), "TASK-REL");

            vehicle.ReleaseTask();

            vehicle.Status.ShouldBe(VehicleStatus.Idle);
            vehicle.CurrentTaskId.ShouldBeNull();
            vehicle.CurrentTaskCode.ShouldBeNull();
        }

        [Fact]
        public void ReportError_And_Reset_Should_Transition_Properly()
        {
            var vehicle = new AgvVehicle(Guid.NewGuid(), "AGV-RESET-01");
            vehicle.AssignTask(Guid.NewGuid(), "TASK-WILL-FAIL");

            vehicle.ReportError("急停按钮被拍下");
            vehicle.Status.ShouldBe(VehicleStatus.Error);
            vehicle.ErrorMessage.ShouldBe("急停按钮被拍下");

            vehicle.Reset("调度员现场确认安全，人工复位");
            vehicle.Status.ShouldBe(VehicleStatus.Idle);
            vehicle.ErrorMessage.ShouldBeNull();
            vehicle.CurrentTaskId.ShouldBeNull();
            vehicle.CurrentTaskCode.ShouldBeNull();
        }

        [Fact]
        public void UpdateTelemetry_Should_Clamp_Battery_And_Update_Station()
        {
            var vehicle = new AgvVehicle(Guid.NewGuid(), "AGV-TELEM-01");

            vehicle.UpdateTelemetry("STATION-NEW", 120.0, "192.168.1.99");
            vehicle.BatteryPercentage.ShouldBe(100.0); // 截断到 100
            vehicle.CurrentStation.ShouldBe("STATION-NEW");
            vehicle.IpAddress.ShouldBe("192.168.1.99");

            vehicle.UpdateTelemetry(null, -10.0, null);
            vehicle.BatteryPercentage.ShouldBe(0.0); // 截断到 0
            vehicle.CurrentStation.ShouldBe("STATION-NEW"); // 未传不修改
        }
    }
}
