namespace SIASUN.RCS.Commands
{
    /// <summary>
    /// 支持自动审计上下文元数据的领域命令契约接口
    /// 实现此接口的命令将在 CQRS 管道中被自动解剖并全自动记录 OperationLog，杜绝业务代码手写日志
    /// </summary>
    public interface IAuditableCommand
    {
        /// <summary>
        /// 业务所属模块（如 "Dispatch", "Task", "Fleet", "System"）
        /// </summary>
        string Module { get; }

        /// <summary>
        /// 业务动作标识（如 "CancelTask", "ForceEndTask", "AssignVehicle", "ResetVehicle"）
        /// </summary>
        string Action { get; }

        /// <summary>
        /// 操作目标对象类型（如 "Task", "Vehicle", "Station"）
        /// </summary>
        string TargetType { get; }

        /// <summary>
        /// 操作目标唯一业务标识（如 TaskCode、VehicleCode）
        /// </summary>
        string? TargetId { get; }

        /// <summary>
        /// 关联的任务编号（若有）
        /// </summary>
        string? TaskId => TargetType == "Task" ? TargetId : null;

        /// <summary>
        /// 关联的 AGV 编号（若有）
        /// </summary>
        string? AgvId => TargetType == "Vehicle" ? TargetId : null;

        /// <summary>
        /// 操作原因或排障处置依据（工控现场定分止争核心字段）
        /// </summary>
        string? Reason { get; }

        /// <summary>
        /// 操作业务描述
        /// </summary>
        string? Description { get; }
    }
}

