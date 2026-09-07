using SIASUN.RCS.Commands;
using SIASUN.RCS.Dispatch.Dtos;

namespace SIASUN.RCS.Dispatch.Commands
{
    /// <summary>
    /// 调度员人工指派车辆执行任务命令
    /// </summary>
    /// <param name="TaskId">任务唯一编号或主键</param>
    /// <param name="AgvId">目标车辆唯一编号或主键</param>
    /// <param name="Reason">指派原因（必填）</param>
    public record AssignVehicleCommand(
        string TaskId,
        string AgvId,
        string Reason) : ICommand<DispatchInterventionResultDto>, IAuditableCommand
    {
        string IAuditableCommand.Module => "Dispatch";
        string IAuditableCommand.Action => "AssignVehicle";
        string IAuditableCommand.TargetType => "Task";
        string? IAuditableCommand.TargetId => TaskId;
        string? IAuditableCommand.TaskId => TaskId;
        string? IAuditableCommand.AgvId => AgvId;
        string? IAuditableCommand.Reason => Reason;
        string? IAuditableCommand.Description => $"调度员指定车辆 [{AgvId}] 执行任务 [{TaskId}]，原因：{Reason}";
    }
}

