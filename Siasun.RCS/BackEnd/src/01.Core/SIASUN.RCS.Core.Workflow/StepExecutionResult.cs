using System;

namespace SIASUN.RCS.Tasks.Workflow
{
    /// <summary>
    /// 步骤执行结果状态枚举
    /// </summary>
    public enum StepStatus
    {
        /// <summary>
        /// 步骤执行成功，推进至下一步
        /// </summary>
        Success,

        /// <summary>
        /// 步骤挂起，进入异步等待外部信号状态（如等待 TM 回调或 PLC 对齐）
        /// </summary>
        Suspended,

        /// <summary>
        /// 步骤发生瞬态故障，请求重试（仅对可幂等步进生效）
        /// </summary>
        Retry,

        /// <summary>
        /// 步骤不可恢复失败或非幂等步进发生异常
        /// </summary>
        Failed
    }

    /// <summary>
    /// 工作流步骤执行结果对象
    /// </summary>
    public class StepExecutionResult
    {
        /// <summary>
        /// 执行结果状态
        /// </summary>
        public StepStatus Status { get; }

        /// <summary>
        /// 推进的目标步骤序号（可选，为空则默认 stepIndex + 1）
        /// </summary>
        public int? NextStepIndex { get; }

        /// <summary>
        /// 等待的外部事件/信号标识
        /// </summary>
        public string? WaitingEvent { get; }

        /// <summary>
        /// 等待超时时限
        /// </summary>
        public TimeSpan? Timeout { get; }

        /// <summary>
        /// 错误原因或说明
        /// </summary>
        public string? Message { get; }

        private StepExecutionResult(StepStatus status, int? nextStepIndex, string? waitingEvent, TimeSpan? timeout, string? message)
        {
            Status = status;
            NextStepIndex = nextStepIndex;
            WaitingEvent = waitingEvent;
            Timeout = timeout;
            Message = message;
        }

        /// <summary>
        /// 创建成功推进结果
        /// </summary>
        /// <param name="nextStepIndex">可选目标下一步序号</param>
        /// <param name="message">成功说明</param>
        public static StepExecutionResult Success(int? nextStepIndex = null, string? message = null)
        {
            return new StepExecutionResult(StepStatus.Success, nextStepIndex, null, null, message);
        }

        /// <summary>
        /// 创建挂起等待外部信号结果
        /// </summary>
        /// <param name="waitingEvent">等待事件标识</param>
        /// <param name="timeout">可选超时时限</param>
        public static StepExecutionResult Suspend(string waitingEvent, TimeSpan? timeout = null)
        {
            return new StepExecutionResult(StepStatus.Suspended, null, waitingEvent, timeout, null);
        }

        /// <summary>
        /// 创建瞬态重试结果（仅允许在可幂等步进中重试）
        /// </summary>
        /// <param name="reason">重试原因</param>
        public static StepExecutionResult Retry(string reason)
        {
            return new StepExecutionResult(StepStatus.Retry, null, null, null, reason);
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        /// <param name="reason">失败根因</param>
        public static StepExecutionResult Fail(string reason)
        {
            return new StepExecutionResult(StepStatus.Failed, null, null, null, reason);
        }
    }
}
