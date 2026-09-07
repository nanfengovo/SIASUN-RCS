using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using SIASUN.RCS.Dispatch.Commands;
using SIASUN.RCS.Dispatch.Dtos;
using SIASUN.RCS.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace SIASUN.RCS.Dispatch
{
    /// <summary>
    /// 调度员人工干预应用服务实现（极薄 API 门面）
    /// 承载现场调度员对生产现场异常任务与车辆的人工干预，通过 CQRS 管道自动生成不可抵赖的定分止争责任审计
    /// 业务逻辑委托给强类型 CommandHandler 处理，服务内部零手写日志代码
    /// </summary>
    [Authorize(RCSPermissions.DispatchIntervention.Default)]
    public class DispatchInterventionAppService : ApplicationService, IDispatchInterventionAppService
    {
        private readonly IMediator _mediator;

        /// <summary>
        /// 构造函数注入 MediatR 中介者
        /// </summary>
        /// <param name="mediator">MediatR 实例</param>
        public DispatchInterventionAppService(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// 调度员人工干预取消指定的调度任务
        /// </summary>
        /// <param name="input">取消任务参数</param>
        /// <returns>干预操作结果</returns>
        [Authorize(RCSPermissions.DispatchIntervention.Cancel)]
        public async Task<DispatchInterventionResultDto> CancelTaskAsync(CancelTaskInput input)
        {
            Check.NotNull(input, nameof(input));
            return await _mediator.Send(new CancelTaskCommand(input.TaskId, input.Reason, input.AgvId));
        }

        /// <summary>
        /// 调度员人工强制完结指定的调度任务
        /// </summary>
        /// <param name="input">强制完结参数</param>
        /// <returns>干预操作结果</returns>
        [Authorize(RCSPermissions.DispatchIntervention.ForceEnd)]
        public async Task<DispatchInterventionResultDto> ForceEndTaskAsync(ForceEndTaskInput input)
        {
            Check.NotNull(input, nameof(input));
            return await _mediator.Send(new ForceEndTaskCommand(input.TaskId, input.Reason, input.AgvId));
        }

        /// <summary>
        /// 调度员人工指定特定 AGV 车辆执行任务
        /// </summary>
        /// <param name="input">指派车辆参数</param>
        /// <returns>干预操作结果</returns>
        [Authorize(RCSPermissions.DispatchIntervention.Assign)]
        public async Task<DispatchInterventionResultDto> AssignVehicleAsync(AssignVehicleInput input)
        {
            Check.NotNull(input, nameof(input));
            return await _mediator.Send(new AssignVehicleCommand(input.TaskId, input.AgvId, input.Reason));
        }

        /// <summary>
        /// 调度员人工复位处于异常或告警状态的 AGV 车辆
        /// </summary>
        /// <param name="input">复位车辆参数</param>
        /// <returns>干预操作结果</returns>
        [Authorize(RCSPermissions.DispatchIntervention.Reset)]
        public async Task<DispatchInterventionResultDto> ResetVehicleAsync(ResetVehicleInput input)
        {
            Check.NotNull(input, nameof(input));
            return await _mediator.Send(new ResetVehicleCommand(input.AgvId, input.Reason));
        }
    }
}
