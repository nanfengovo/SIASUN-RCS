namespace SIASUN.RCS.Commands
{
    /// <summary>
    /// 支持携带实体状态变迁快照的命令执行结果契约接口
    /// 实现此接口的命令返回值将在 CQRS 管道中被自动提取 BeforeState / AfterState 并记录到操作审计
    /// </summary>
    public interface IStateTransitionResult
    {
        /// <summary>
        /// 变迁前状态快照（如 "Running"）
        /// </summary>
        string? BeforeState { get; }

        /// <summary>
        /// 变迁后状态快照（如 "Canceled"）
        /// </summary>
        string? AfterState { get; }

        /// <summary>
        /// 目标对象类型（可选，覆盖命令中的 TargetType）
        /// </summary>
        string? TargetType { get; }

        /// <summary>
        /// 目标对象唯一标识（可选，覆盖命令中的 TargetId）
        /// </summary>
        string? TargetId { get; }
    }
}

