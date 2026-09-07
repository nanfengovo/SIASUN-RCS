using SIASUN.RCS.Commands;
using SIASUN.RCS.Dispatch.Dtos;

namespace SIASUN.RCS.Dispatch.Commands
{
    /// <summary>
    /// 调度员人工干预取消任务命令
    /// </summary>
    /// <param name="TaskId">任务唯一编号或主键</param>
    /// <param name="Reason">人工干预取消原因（必填）</param>
    /// <param name="AgvId">关联的 AGV 编号（可选）</param>
    public record CancelTaskCommand(
        string TaskId,
        string Reason,
        string? AgvId = null) : ICommand<DispatchInterventionResultDto>, IAuditableCommand
    {
        string IAuditableCommand.Module => "Dispatch";
        string IAuditableCommand.Action => "CancelTask";
        string IAuditableCommand.TargetType => "Task";
        string? IAuditableCommand.TargetId => TaskId;
        string? IAuditableCommand.TaskId => TaskId;
        string? IAuditableCommand.AgvId => AgvId;
        string? IAuditableCommand.Reason => Reason;
        string? IAuditableCommand.Description => $"调度员人工干预取消任务 [{TaskId}]，原因：{Reason}";
    }
}

