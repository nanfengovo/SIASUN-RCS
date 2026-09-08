using System;

namespace SIASUN.RCS.Tasks.Workflow
{
    /// <summary>
    /// 工作流可调度任务契约接口
    /// 遵循微内核架构哲学：调度微内核依赖抽象任务契约，与特定 ORM 实体彻底解耦
    /// </summary>
    public interface IWorkflowTask
    {
        /// <summary>任务唯一标识</summary>
        Guid Id { get; }

        /// <summary>业务任务编号</summary>
        string TaskCode { get; }

        /// <summary>5状态粗粒度任务生命周期状态</summary>
        AgvTaskStatus Status { get; }

        /// <summary>细粒度工作流步骤序号</summary>
        int StepIndex { get; }

        /// <summary>当前激活程段</summary>
        string? ActiveLeg { get; }

        /// <summary>挂起等待外部事件标识</summary>
        string? WaitingEvent { get; }

        /// <summary>当前步骤已重试次数</summary>
        int RetryCount { get; }

        /// <summary>最大允许重试次数</summary>
        int MaxRetryCount { get; }

        /// <summary>全链路追踪标识</summary>
        string? TraceId { get; }

        /// <summary>正常完结任务</summary>
        void Complete(DateTime? endTime = null);

        /// <summary>标记任务失败</summary>
        void Fail(string reason);

        /// <summary>步骤推进</summary>
        void AdvanceStep(int stepIndex, string? activeLeg = null, string? waitingEvent = null);

        /// <summary>挂起当前步骤</summary>
        void Suspend(string waitingEvent);

        /// <summary>外部信号唤醒</summary>
        void ResumeByEvent(string receivedEvent);

        /// <summary>记录步骤重试</summary>
        void RecordRetry(string stepName, string errorMessage, bool isIdempotent = true);

        /// <summary>SAGA 补偿回退</summary>
        void RollbackToStep(int targetStepIndex, string reason);
    }
}
