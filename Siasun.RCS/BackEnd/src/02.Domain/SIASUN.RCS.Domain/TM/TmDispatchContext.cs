using System;

namespace SIASUN.RCS.TM
{
    /// <summary>
    /// TM 搬运指令下发上下文
    /// </summary>
    public class TmDispatchContext
    {
        /// <summary>
        /// 内部 AGV 任务实体 ID
        /// </summary>
        public Guid TaskId { get; set; }

        /// <summary>
        /// 内部任务编号（如 TSK-202609080001）
        /// </summary>
        public string TaskCode { get; set; } = string.Empty;

        /// <summary>
        /// 当前激活的航段（如 "Fetch"、"Put"、"Move"）
        /// </summary>
        public string Leg { get; set; } = string.Empty;

        /// <summary>
        /// 当前工作流步进索引
        /// </summary>
        public int StepIndex { get; set; }

        /// <summary>
        /// 执行车体编号（如 "AGV-01"）
        /// </summary>
        public string VehicleCode { get; set; } = string.Empty;

        /// <summary>
        /// 经 Schema 动态装配并编译固化的 32 位 OptionCode 报文字符串
        /// </summary>
        public string? OptionCode { get; set; }

        /// <summary>
        /// 目标站点或工位编码
        /// </summary>
        public string? TargetStation { get; set; }

        /// <summary>
        /// 关联等待唤醒的异步事件标识（如 "TM_FETCH_DONE"）
        /// </summary>
        public string? WaitingEvent { get; set; }

        /// <summary>
        /// 全链路贯穿 TraceId
        /// </summary>
        public string? TraceId { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public TmDispatchContext(
            Guid taskId,
            string taskCode,
            string leg,
            int stepIndex,
            string vehicleCode,
            string? optionCode = null,
            string? targetStation = null,
            string? waitingEvent = null,
            string? traceId = null)
        {
            TaskId = taskId;
            TaskCode = taskCode;
            Leg = leg;
            StepIndex = stepIndex;
            VehicleCode = vehicleCode;
            OptionCode = optionCode;
            TargetStation = targetStation;
            WaitingEvent = waitingEvent;
            TraceId = traceId;
        }
    }
}
