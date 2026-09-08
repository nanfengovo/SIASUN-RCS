using System.Threading.Tasks;
using SIASUN.RCS.Dispatch.Dtos;
using Volo.Abp.Application.Services;

namespace SIASUN.RCS.Dispatch
{
    /// <summary>
    /// 调度员人工干预应用服务契约接口
    /// 提供控制台/大屏调度员对异常任务与车辆的人工干预能力，全量记录责任审计
    /// </summary>
    public interface IDispatchInterventionAppService : IApplicationService
    {
        /// <summary>
        /// 调度员人工干预取消指定的调度任务
        /// </summary>
        /// <param name="input">取消任务参数（含任务号与干预原因）</param>
        /// <returns>干预操作结果</returns>
        Task<DispatchInterventionResultDto> CancelTaskAsync(CancelTaskInput input);

        /// <summary>
        /// 调度员人工强制完结指定的调度任务
        /// </summary>
        /// <param name="input">强制完结参数（含任务号与干预原因）</param>
        /// <returns>干预操作结果</returns>
        Task<DispatchInterventionResultDto> ForceEndTaskAsync(ForceEndTaskInput input);

        /// <summary>
        /// 调度员人工指定特定 AGV 车辆执行任务
        /// </summary>
        /// <param name="input">指派车辆参数（含任务号、车辆编号与原因）</param>
        /// <returns>干预操作结果</returns>
        Task<DispatchInterventionResultDto> AssignVehicleAsync(AssignVehicleInput input);

        /// <summary>
        /// 调度员人工复位处于异常或告警状态的 AGV 车辆
        /// </summary>
        /// <param name="input">复位车辆参数（含车辆编号与原因）</param>
        /// <returns>干预操作结果</returns>
        Task<DispatchInterventionResultDto> ResetVehicleAsync(ResetVehicleInput input);

        /// <summary>
        /// 调度员人工干预恢复失败的任务（断点重试/显式状态复原）
        /// </summary>
        /// <param name="input">恢复任务参数（含任务号、是否重试当前步与原因）</param>
        /// <returns>干预操作结果</returns>
        Task<DispatchInterventionResultDto> ResumeTaskAsync(ResumeTaskInput input);

        /// <summary>
        /// 调度员人工干预回滚并重试指定的任务（SAGA 补偿与安全回退）
        /// </summary>
        /// <param name="input">回滚重试参数（含任务号、目标步骤索引与原因）</param>
        /// <returns>干预操作结果</returns>
        Task<DispatchInterventionResultDto> RollbackAndRetryAsync(RollbackAndRetryInput input);
    }
}

