using SIASUN.RCS.Commands;
using SIASUN.RCS.Dispatch.Dtos;

namespace SIASUN.RCS.Dispatch.Commands
{
    /// <summary>
    /// 调度员人工复位处于异常或告警状态的车辆命令
    /// </summary>
    /// <param name="AgvId">车辆唯一编号或主键</param>
    /// <param name="Reason">复位原因与处置依据（必填）</param>
    public record ResetVehicleCommand(
        string AgvId,
        string Reason) : ICommand<DispatchInterventionResultDto>, IAuditableCommand
    {
        string IAuditableCommand.Module => "Dispatch";
        string IAuditableCommand.Action => "ResetVehicle";
        string IAuditableCommand.TargetType => "Vehicle";
        string? IAuditableCommand.TargetId => AgvId;
        string? IAuditableCommand.TaskId => null;
        string? IAuditableCommand.AgvId => AgvId;
        string? IAuditableCommand.Reason => Reason;
        string? IAuditableCommand.Description => $"调度员人工复位车辆 [{AgvId}]，原因：{Reason}";
    }
}

