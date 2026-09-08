using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using SIASUN.RCS.Diagnostics;
using SIASUN.RCS.Dispatch;
using SIASUN.RCS.Dispatch.Dtos;
using SIASUN.RCS.EntityFrameworkCore;
using SIASUN.RCS.Tasks;
using SIASUN.RCS.Vehicles;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace SIASUN.RCS.EntityFrameworkCore.Applications
{
    /// <summary>
    /// 调度员人工干预真实数据库落库集成测试（L3 可证核心验收）
    /// 验证调度干预操作对真实聚合根实体 AgvTask 与 AgvVehicle 进行状态迁移与持久化更新
    /// </summary>
    [Collection(RCSTestConsts.CollectionDefinitionName)]
    public class DispatchInterventionDatabaseIntegrationTests : RCSEntityFrameworkCoreTestBase
    {
        private readonly IDispatchInterventionAppService _appService;
        private readonly IRepository<AgvTask, Guid> _taskRepository;
        private readonly IRepository<AgvVehicle, Guid> _vehicleRepository;
        private readonly RCSDbContext _dbContext;

        public DispatchInterventionDatabaseIntegrationTests()
        {
            _appService = GetRequiredService<IDispatchInterventionAppService>();
            _taskRepository = GetRequiredService<IRepository<AgvTask, Guid>>();
            _vehicleRepository = GetRequiredService<IRepository<AgvVehicle, Guid>>();
            _dbContext = GetRequiredService<RCSDbContext>();
        }

        [Fact]
        public async Task CancelTaskAsync_Should_Mutate_Real_AgvTask_In_Database()
        {
            // Arrange
            var testTraceId = "TRACE-DB-CANCEL-001";
            using var traceScope = RcsTraceContext.SetScoped(testTraceId);

            var taskId = Guid.NewGuid();
            var task = new AgvTask(taskId, "TASK-DB-001", "ST-01", "ST-02");
            var vehicleId = Guid.NewGuid();
            var vehicle = new AgvVehicle(vehicleId, "AGV-01");

            await _taskRepository.InsertAsync(task, autoSave: true);
            await _vehicleRepository.InsertAsync(vehicle, autoSave: true);

            // 启动任务为 Running
            task.Start(vehicleId, "AGV-01", testTraceId);
            await _taskRepository.UpdateAsync(task, autoSave: true);

            var dbTaskBefore = await _dbContext.AgvTasks.AsNoTracking().FirstAsync(x => x.Id == taskId);
            dbTaskBefore.Status.ShouldBe(AgvTaskStatus.Running);

            // Act: 调度员人工取消任务
            var result = await _appService.CancelTaskAsync(new CancelTaskInput
            {
                TaskId = "TASK-DB-001",
                AgvId = "AGV-01",
                Reason = "现场库位光电遮挡，调度员紧急取消"
            });

            // Assert: 内存返回值
            result.Success.ShouldBeTrue();
            result.CurrentState.ShouldBe(AgvTaskStatus.Canceled.ToString());

            // Assert: 真实数据库持久化状态验证
            var dbTaskAfter = await _dbContext.AgvTasks.AsNoTracking().FirstAsync(x => x.Id == taskId);
            dbTaskAfter.Status.ShouldBe(AgvTaskStatus.Canceled);
            dbTaskAfter.FailureReason.ShouldBe("现场库位光电遮挡，调度员紧急取消");
            dbTaskAfter.EndTime.ShouldNotBeNull();
        }

        [Fact]
        public async Task ResetVehicleAsync_Should_Mutate_Real_AgvVehicle_In_Database()
        {
            // Arrange
            var testTraceId = "TRACE-DB-RESET-001";
            using var traceScope = RcsTraceContext.SetScoped(testTraceId);

            var vehicleId = Guid.NewGuid();
            var vehicle = new AgvVehicle(vehicleId, "AGV-ALARM-99");
            vehicle.ReportError("底盘雷达急停触发");
            await _vehicleRepository.InsertAsync(vehicle, autoSave: true);

            var dbVehicleBefore = await _dbContext.AgvVehicles.AsNoTracking().FirstAsync(x => x.Id == vehicleId);
            dbVehicleBefore.Status.ShouldBe(VehicleStatus.Error);
            dbVehicleBefore.ErrorMessage.ShouldBe("底盘雷达急停触发");

            // Act: 调度员人工复位
            var result = await _appService.ResetVehicleAsync(new ResetVehicleInput
            {
                AgvId = "AGV-ALARM-99",
                Reason = "现场安全隐患已排除，雷达恢复正常，调度员手动复位"
            });

            // Assert
            result.Success.ShouldBeTrue();
            result.CurrentState.ShouldBe(VehicleStatus.Idle.ToString());

            var dbVehicleAfter = await _dbContext.AgvVehicles.AsNoTracking().FirstAsync(x => x.Id == vehicleId);
            dbVehicleAfter.Status.ShouldBe(VehicleStatus.Idle);
            dbVehicleAfter.ErrorMessage.ShouldBeNull();
        }

        [Fact]
        public async Task AssignVehicleAsync_Should_Bind_Task_And_Vehicle_In_Database()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var task = new AgvTask(taskId, "TASK-ASSIGN-001", "ST-A", "ST-B");
            await _taskRepository.InsertAsync(task, autoSave: true);

            var vehicleId = Guid.NewGuid();
            var vehicle = new AgvVehicle(vehicleId, "AGV-FREE-07");
            await _vehicleRepository.InsertAsync(vehicle, autoSave: true);

            // Act: 调度员指定特定车辆执行任务
            var result = await _appService.AssignVehicleAsync(new AssignVehicleInput
            {
                TaskId = "TASK-ASSIGN-001",
                AgvId = "AGV-FREE-07",
                Reason = "产线节拍均衡，人工指派专车"
            });

            // Assert
            result.Success.ShouldBeTrue();
            result.CurrentState.ShouldBe("Assigned:AGV-FREE-07");

            var dbTask = await _dbContext.AgvTasks.FirstAsync(x => x.Id == taskId);
            dbTask.AssignedVehicleId.ShouldBe(vehicleId);
            dbTask.AssignedVehicleCode.ShouldBe("AGV-FREE-07");

            var dbVehicle = await _dbContext.AgvVehicles.FirstAsync(x => x.Id == vehicleId);
            dbVehicle.Status.ShouldBe(VehicleStatus.Running);
            dbVehicle.CurrentTaskId.ShouldBe(taskId);
            dbVehicle.CurrentTaskCode.ShouldBe("TASK-ASSIGN-001");
        }

        [Fact]
        public async Task ResumeTaskAsync_Should_Persist_Running_State_In_Database()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var task = new AgvTask(taskId, "TASK-DB-RESUME", "ST-01", "ST-02");
            var vehicleId = Guid.NewGuid();
            var vehicle = new AgvVehicle(vehicleId, "AGV-05");

            await _taskRepository.InsertAsync(task, autoSave: true);
            await _vehicleRepository.InsertAsync(vehicle, autoSave: true);

            task.Start(vehicleId, "AGV-05");
            task.Fail("门磁检测超时");
            await _taskRepository.UpdateAsync(task, autoSave: true);

            // Act: 调度员人工干预恢复任务
            var result = await _appService.ResumeTaskAsync(new ResumeTaskInput
            {
                TaskId = "TASK-DB-RESUME",
                Reason = "门磁故障已现场排查，恢复任务执行",
                RetryCurrentStep = true
            });

            // Assert
            result.Success.ShouldBeTrue();
            var dbTask = await _dbContext.AgvTasks.AsNoTracking().FirstAsync(x => x.Id == taskId);
            dbTask.Status.ShouldBe(AgvTaskStatus.Running);
            dbTask.FailureReason.ShouldBeNull();
        }

        [Fact]
        public async Task RollbackAndRetryAsync_Should_Persist_Step_And_Status_In_Database()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var task = new AgvTask(taskId, "TASK-DB-ROLLBACK", "ST-01", "ST-02");
            var vehicleId = Guid.NewGuid();
            var vehicle = new AgvVehicle(vehicleId, "AGV-06");

            await _taskRepository.InsertAsync(task, autoSave: true);
            await _vehicleRepository.InsertAsync(vehicle, autoSave: true);

            task.Start(vehicleId, "AGV-06");
            task.AdvanceStep(5, "Put");
            await _taskRepository.UpdateAsync(task, autoSave: true);

            // Act: 调度员人工回滚至第 2 步
            var result = await _appService.RollbackAndRetryAsync(new RollbackAndRetryInput
            {
                TaskId = "TASK-DB-ROLLBACK",
                TargetStepIndex = 2,
                Reason = "目标位传感器存在杂物遮挡，清理后回滚至二次对位重试"
            });

            // Assert
            result.Success.ShouldBeTrue();
            var dbTask = await _dbContext.AgvTasks.AsNoTracking().FirstAsync(x => x.Id == taskId);
            dbTask.Status.ShouldBe(AgvTaskStatus.Running);
            dbTask.StepIndex.ShouldBe(2);
        }
    }
}
