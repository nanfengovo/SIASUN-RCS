using SIASUN.RCS.Commands;
using SIASUN.RCS.Dispatch.Dtos;

namespace SIASUN.RCS.Dispatch.Commands
{
    /// <summary>
    /// 调度员人工干预回滚并重试任务命令（SAGA 补偿与安全回退）
    /// </summary>
    /// <param name="TaskId">任务编号或主键</param>
    /// <param name="TargetStepIndex">目标回滚步骤序号</param>
    /// <param name="Reason">人工干预回滚原因（必填）</param>
    /// <param name="AgvId">关联的 AGV 编号（可选）</param>
    public record RollbackAndRetryCommand(
        string TaskId,
        int TargetStepIndex,
        string Reason,
        string? AgvId = null) : ICommand<DispatchInterventionResultDto>, IAuditableCommand
    {
        string IAuditableCommand.Module => "Dispatch";
        string IAuditableCommand.Action => "RollbackAndRetry";
        string IAuditableCommand.TargetType => "Task";
        string? IAuditableCommand.TargetId => TaskId;
        string? IAuditableCommand.TaskId => TaskId;
        string? IAuditableCommand.AgvId => AgvId;
        string? IAuditableCommand.Reason => Reason;
        string? IAuditableCommand.Description => $"调度员人工回滚任务 [{TaskId}] 至第 {TargetStepIndex} 步重试，原因：{Reason}";
    }
}
