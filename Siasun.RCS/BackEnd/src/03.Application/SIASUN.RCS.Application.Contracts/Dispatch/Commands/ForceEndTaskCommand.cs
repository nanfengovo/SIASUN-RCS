using SIASUN.RCS.Commands;
using SIASUN.RCS.Dispatch.Dtos;

namespace SIASUN.RCS.Dispatch.Commands
{
    /// <summary>
    /// 调度员人工强制完结任务命令
    /// </summary>
    /// <param name="TaskId">任务唯一编号或主键</param>
    /// <param name="Reason">人工强制完结原因（必填）</param>
    /// <param name="AgvId">关联的 AGV 编号（可选）</param>
    public record ForceEndTaskCommand(
        string TaskId,
        string Reason,
        string? AgvId = null) : ICommand<DispatchInterventionResultDto>, IAuditableCommand
    {
        string IAuditableCommand.Module => "Dispatch";
        string IAuditableCommand.Action => "ForceEndTask";
        string IAuditableCommand.TargetType => "Task";
        string? IAuditableCommand.TargetId => TaskId;
        string? IAuditableCommand.TaskId => TaskId;
        string? IAuditableCommand.AgvId => AgvId;
        string? IAuditableCommand.Reason => Reason;
        string? IAuditableCommand.Description => $"调度员人工强制完结任务 [{TaskId}]，原因：{Reason}";
    }
}

