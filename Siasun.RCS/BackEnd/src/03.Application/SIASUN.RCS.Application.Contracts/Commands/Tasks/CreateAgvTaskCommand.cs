using System;
using SIASUN.RCS.Commands;
using SIASUN.RCS.Tasks.Dtos;

namespace SIASUN.RCS.Commands.Tasks
{
    /// <summary>
    /// 创建新 AGV 搬运任务 CQRS 命令
    /// 纳入 CQRS 与 OperationLog 审计流水线，提供全局任务创建唯一性保障与操作审计
    /// </summary>
    public record CreateAgvTaskCommand(
        string TaskCode,
        string FromStation,
        string ToStation,
        string? CarrierCode = null,
        string? BatchId = null,
        string WorkflowKey = "transfer_standard",
        string? TraceId = null,
        string? Reason = "创建新搬运任务") : ICommand<AgvTaskDto>, IAuditableCommand
    {
        string IAuditableCommand.Module => "TaskDispatch";
        string IAuditableCommand.Action => "CreateTask";
        string IAuditableCommand.TargetType => "Task";
        string? IAuditableCommand.TargetId => TaskCode;
        string? IAuditableCommand.TaskId => TaskCode;
        string? IAuditableCommand.AgvId => null;
        string? IAuditableCommand.Reason => Reason;
        string? IAuditableCommand.Description => $"创建调度任务 [{TaskCode}], 起点={FromStation}, 终点={ToStation}, 载具={CarrierCode ?? "N/A"}, 工作流={WorkflowKey}";
    }
}
