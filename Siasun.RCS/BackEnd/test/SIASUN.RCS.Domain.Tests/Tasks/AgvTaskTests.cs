using System;
using System.Linq;
using Shouldly;
using SIASUN.RCS.Tasks;
using Volo.Abp;
using Xunit;

namespace SIASUN.RCS.Domain.Tests.Tasks
{
    /// <summary>
    /// AGV 调度任务领域模型单元测试
    /// 严格验证 SIASUN RCS 5 状态生命周期、细粒度步进推进、领域事件解耦以及状态防护规则
    /// </summary>
    public class AgvTaskTests
    {
        [Fact]
        public void Constructor_Should_Initialize_With_Pending_Status()
        {
            var id = Guid.NewGuid();
            var task = new AgvTask(
                id,
                "TASK-2026-001",
                fromStation: "ST-PICK-01",
                toStation: "ST-DROP-02",
                carrierCode: "CARRIER-FOUP-99",
                batchId: "BATCH-888",
                optionCode: "MOLDING_V1_FETCH_PUT",
                traceId: "TRACE-001");

            task.Id.ShouldBe(id);
            task.TaskCode.ShouldBe("TASK-2026-001");
            task.Status.ShouldBe(AgvTaskStatus.Pending);
            task.StepIndex.ShouldBe(0);
            task.FromStation.ShouldBe("ST-PICK-01");
            task.ToStation.ShouldBe("ST-DROP-02");
            task.CarrierCode.ShouldBe("CARRIER-FOUP-99");
            task.BatchId.ShouldBe("BATCH-888");
            task.OptionCode.ShouldBe("MOLDING_V1_FETCH_PUT");
            task.TraceId.ShouldBe("TRACE-001");
            task.AssignedVehicleId.ShouldBeNull();
            task.AssignedVehicleCode.ShouldBeNull();
            task.StartTime.ShouldBeNull();
            task.EndTime.ShouldBeNull();
        }

        [Fact]
        public void Start_Should_Transition_To_Running_And_Bind_Vehicle()
        {
            var task = new AgvTask(Guid.NewGuid(), "TASK-START-01");
            var vehicleId = Guid.NewGuid();

            task.Start(vehicleId, "AGV-01", "TRACE-START-01");

            task.Status.ShouldBe(AgvTaskStatus.Running);
            task.AssignedVehicleId.ShouldBe(vehicleId);
            task.AssignedVehicleCode.ShouldBe("AGV-01");
            task.TraceId.ShouldBe("TRACE-START-01");
            task.StartTime.ShouldNotBeNull();
            task.StepIndex.ShouldBe(1);
        }

        [Fact]
        public void Start_When_Not_Pending_Should_Throw_BusinessException()
        {
            var task = new AgvTask(Guid.NewGuid(), "TASK-START-02");
            task.Start(Guid.NewGuid(), "AGV-01");

            // 再次 Start 应当抛出异常
            var ex = Should.Throw<BusinessException>(() =>
            {
                task.Start(Guid.NewGuid(), "AGV-02");
            });

            ex.Code.ShouldBe("RCS:TaskCannotStart");
        }

        [Fact]
        public void AdvanceStep_Should_Update_StepIndex_ActiveLeg_And_WaitingEvent()
        {
            var task = new AgvTask(Guid.NewGuid(), "TASK-ADVANCE-01");
            task.Start(Guid.NewGuid(), "AGV-01");

            task.AdvanceStep(2, activeLeg: "Fetch", waitingEvent: "PlcDoorOpen");

            task.StepIndex.ShouldBe(2);
            task.ActiveLeg.ShouldBe("Fetch");
            task.WaitingEvent.ShouldBe("PlcDoorOpen");

            task.AdvanceStep(3, activeLeg: "Put", waitingEvent: null);
            task.StepIndex.ShouldBe(3);
            task.ActiveLeg.ShouldBe("Put");
            task.WaitingEvent.ShouldBeNull();
        }

        [Fact]
        public void AdvanceStep_When_Not_Running_Should_Throw_BusinessException()
        {
            var task = new AgvTask(Guid.NewGuid(), "TASK-ADVANCE-ERR");

            var ex = Should.Throw<BusinessException>(() =>
            {
                task.AdvanceStep(2);
            });

            ex.Code.ShouldBe("RCS:TaskNotInRunningState");
        }

        [Fact]
        public void AssignVehicle_Should_Update_Vehicle_Without_Corrupting_FailureReason()
        {
            var task = new AgvTask(Guid.NewGuid(), "TASK-REASSIGN-01");
            var vehicle1Id = Guid.NewGuid();
            task.Start(vehicle1Id, "AGV-01");

            var vehicle2Id = Guid.NewGuid();
            task.AssignVehicle(vehicle2Id, "AGV-02", "调度员重新指派");

            task.AssignedVehicleId.ShouldBe(vehicle2Id);
            task.AssignedVehicleCode.ShouldBe("AGV-02");
            task.FailureReason.ShouldBeNull(); // 绝不能将指派原因写入 FailureReason
        }

        [Fact]
        public void Complete_Should_Set_Succeeded_And_Publish_TaskLifecycleEndedEvent()
        {
            var task = new AgvTask(Guid.NewGuid(), "TASK-COMPLETE-01", traceId: "TRACE-COMP-01");
            task.Start(Guid.NewGuid(), "AGV-01");

            task.Complete();

            task.Status.ShouldBe(AgvTaskStatus.Succeeded);
            task.EndTime.ShouldNotBeNull();
            task.WaitingEvent.ShouldBeNull();

            var events = task.GetLocalEvents();
            events.Count().ShouldBe(1);
            var endedEvent = events.First().EventData.ShouldBeOfType<TaskLifecycleEndedEvent>();
            endedEvent.TaskId.ShouldBe(task.Id);
            endedEvent.TaskCode.ShouldBe("TASK-COMPLETE-01");
            endedEvent.FinalStatus.ShouldBe(AgvTaskStatus.Succeeded);
            endedEvent.Reason.ShouldBeNull();
            endedEvent.TraceId.ShouldBe("TRACE-COMP-01");
            endedEvent.AssignedVehicleCode.ShouldBe("AGV-01");
        }

        [Fact]
        public void Fail_Should_Set_Failed_And_Publish_TaskLifecycleEndedEvent()
        {
            var task = new AgvTask(Guid.NewGuid(), "TASK-FAIL-01", traceId: "TRACE-FAIL-01");
            task.Start(Guid.NewGuid(), "AGV-01");

            task.Fail("底盘激光雷达严重报警");

            task.Status.ShouldBe(AgvTaskStatus.Failed);
            task.FailureReason.ShouldBe("底盘激光雷达严重报警");
            task.EndTime.ShouldNotBeNull();

            var events = task.GetLocalEvents();
            events.Count().ShouldBe(1);
            var endedEvent = events.First().EventData.ShouldBeOfType<TaskLifecycleEndedEvent>();
            endedEvent.FinalStatus.ShouldBe(AgvTaskStatus.Failed);
            endedEvent.Reason.ShouldBe("底盘激光雷达严重报警");
        }

        [Fact]
        public void Cancel_Should_Set_Canceled_And_Publish_TaskLifecycleEndedEvent()
        {
            var task = new AgvTask(Guid.NewGuid(), "TASK-CANCEL-01");
            task.Start(Guid.NewGuid(), "AGV-01");

            task.Cancel("调度员手动取消任务");

            task.Status.ShouldBe(AgvTaskStatus.Canceled);
            task.FailureReason.ShouldBe("调度员手动取消任务");
            task.EndTime.ShouldNotBeNull();

            var events = task.GetLocalEvents();
            events.Count().ShouldBe(1);
            var endedEvent = events.First().EventData.ShouldBeOfType<TaskLifecycleEndedEvent>();
            endedEvent.FinalStatus.ShouldBe(AgvTaskStatus.Canceled);
            endedEvent.Reason.ShouldBe("调度员手动取消任务");
        }

        [Fact]
        public void Cancel_When_Already_Succeeded_Should_Throw_BusinessException()
        {
            var task = new AgvTask(Guid.NewGuid(), "TASK-CANCEL-SUCC");
            task.Start(Guid.NewGuid(), "AGV-01");
            task.Complete();

            var ex = Should.Throw<BusinessException>(() =>
            {
                task.Cancel("试图取消已完成的任务");
            });

            ex.Code.ShouldBe("RCS:CannotCancelSucceededTask");
        }

        [Fact]
        public void ForceEnd_Should_Set_Succeeded_With_Reason_And_Publish_TaskLifecycleEndedEvent()
        {
            var task = new AgvTask(Guid.NewGuid(), "TASK-FORCE-01");
            task.Start(Guid.NewGuid(), "AGV-01");

            task.ForceEnd("现场人工搬离货物，强制完结");

            task.Status.ShouldBe(AgvTaskStatus.Succeeded);
            task.FailureReason.ShouldBe("强制完结: 现场人工搬离货物，强制完结");
            task.EndTime.ShouldNotBeNull();

            var events = task.GetLocalEvents();
            events.Count().ShouldBe(1);
            var endedEvent = events.First().EventData.ShouldBeOfType<TaskLifecycleEndedEvent>();
            endedEvent.FinalStatus.ShouldBe(AgvTaskStatus.Succeeded);
            endedEvent.Reason.ShouldBe("强制完结: 现场人工搬离货物，强制完结");
        }
    }
}
