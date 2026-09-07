using System;
using SIASUN.RCS.Commands;

namespace SIASUN.RCS.Dispatch.Dtos
{
    /// <summary>
    /// 调度人工干预操作响应结果 DTO
    /// 实现 IStateTransitionResult 接口，供 CQRS 管道自动提取状态变迁
    /// </summary>
    public class DispatchInterventionResultDto : IStateTransitionResult
    {
        /// <summary>
        /// 操作是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 操作说明或提示信息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 目标对象类型（Task 或 Vehicle）
        /// </summary>
        public string TargetType { get; set; } = string.Empty;

        /// <summary>
        /// 目标对象唯一编号
        /// </summary>
        public string TargetId { get; set; } = string.Empty;

        /// <summary>
        /// 干预操作前状态快照
        /// </summary>
        public string? BeforeState { get; set; }

        /// <summary>
        /// 干预完成后的新状态（状态变迁后）
        /// </summary>
        public string CurrentState { get; set; } = string.Empty;

        /// <summary>
        /// 状态变迁后快照（实现 IStateTransitionResult）
        /// </summary>
        string? IStateTransitionResult.AfterState => CurrentState;

        /// <summary>
        /// 操作执行时间戳 (UTC)
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}

