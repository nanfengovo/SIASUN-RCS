using SIASUN.RCS.Commands;
using SIASUN.RCS.Dispatch.Dtos;

namespace SIASUN.RCS.Dispatch.Commands
{
    /// <summary>
    /// 调度员人工干预恢复失败任务命令（显式将 Failed 转换为 Running 并恢复步进推进）
    /// </summary>
    /// <param name="TaskId">任务编号或主键</param>
    /// <param name="Reason">人工干预恢复原因（必填）</param>
    /// <param name="RetryCurrentStep">是否重试当前步骤（默认 true）</param>
    /// <param name="AgvId">关联的 AGV 编号（可选）</param>
    public record ResumeTaskCommand(
        string TaskId,
        string Reason,
        bool RetryCurrentStep = true,
        string? AgvId = null) : ICommand<DispatchInterventionResultDto>, IAuditableCommand
    {
        string IAuditableCommand.Module => "Dispatch";
        string IAuditableCommand.Action => "ResumeTask";
        string IAuditableCommand.TargetType => "Task";
        string? IAuditableCommand.TargetId => TaskId;
        string? IAuditableCommand.TaskId => TaskId;
        string? IAuditableCommand.AgvId => AgvId;
        string? IAuditableCommand.Reason => Reason;
        string? IAuditableCommand.Description => $"调度员人工恢复任务 [{TaskId}]，重试当前步={RetryCurrentStep}，原因：{Reason}";
    }
}
