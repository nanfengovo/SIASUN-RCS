using System;
using System.Collections.Generic;

namespace SIASUN.RCS.Tasks.Workflow
{
    /// <summary>
    /// 声明式工作流步骤单元定义
    /// </summary>
    public class WorkflowStepDefinition
    {
        /// <summary>
        /// 步骤流水索引（从 0 开始升序连续递增）
        /// </summary>
        public int StepIndex { get; set; }

        /// <summary>
        /// 步骤名称（例如 "LockPickupLocation", "DispatchFetchLeg", "WaitTmFetchDone"）
        /// </summary>
        public string StepName { get; set; } = string.Empty;

        /// <summary>
        /// 步骤分类类型（例如 "LocationLock", "HardwareGate", "DispatchTmLeg", "WaitForEvent", "OutboxNotify", "Custom"）
        /// </summary>
        public string StepType { get; set; } = string.Empty;

        /// <summary>
        /// 当前步骤激活的航段（例如 "Fetch", "Put", "Move"），若非航段步骤则为 null
        /// </summary>
        public string? ActiveLeg { get; set; }

        /// <summary>
        /// 是否为幂等可重试步骤（true: 瞬态网络或传感器抖动时允许 Polly 自动原步重试；false: 物理动作步骤，重试需 SAGA 补偿或人工确认）
        /// </summary>
        public bool IsIdempotent { get; set; }

        /// <summary>
        /// 该步骤执行或等待的超时阈值（秒），默认 60 秒
        /// </summary>
        public int TimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// 瞬态失败最大自动重试次数（仅在 IsIdempotent=true 时有效），默认 3 次
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// 挂起等待的异步领域/回调信号事件（例如 "TM_FETCH_DONE", "PLC_DOOR_OPENED"）
        /// </summary>
        public string? WaitingEvent { get; set; }

        /// <summary>
        /// 发生严重故障进行 SAGA 补偿回滚时，应回退到的目标步骤索引；若为 null 表示回滚到起点 0
        /// </summary>
        public int? RollbackStepIndex { get; set; }

        /// <summary>
        /// 现场特异性与步骤执行自定义键值参数（例如 { "Action": "Lock", "Target": "FromStation" }）
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public WorkflowStepDefinition()
        {
        }

        /// <summary>
        /// 带参构造函数
        /// </summary>
        public WorkflowStepDefinition(
            int stepIndex,
            string stepName,
            string stepType,
            string? activeLeg = null,
            bool isIdempotent = false,
            int timeoutSeconds = 60,
            int maxRetries = 3,
            string? waitingEvent = null,
            int? rollbackStepIndex = null)
        {
            StepIndex = stepIndex;
            StepName = stepName;
            StepType = stepType;
            ActiveLeg = activeLeg;
            IsIdempotent = isIdempotent;
            TimeoutSeconds = timeoutSeconds;
            MaxRetries = maxRetries;
            WaitingEvent = waitingEvent;
            RollbackStepIndex = rollbackStepIndex;
        }
    }
}
