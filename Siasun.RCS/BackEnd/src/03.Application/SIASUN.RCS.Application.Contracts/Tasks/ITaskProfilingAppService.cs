using System;
using System.Threading.Tasks;
using SIASUN.RCS.Tasks.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SIASUN.RCS.Tasks
{
    /// <summary>
    /// 任务步骤剖析与流转时序看板应用服务契约
    /// </summary>
    public interface ITaskProfilingAppService : IApplicationService
    {
        /// <summary>
        /// 获取指定任务的完整时序流转画像与多系统耗时拆解
        /// 直接支撑前端任务详情大屏弹窗
        /// </summary>
        /// <param name="taskCode">任务唯一编号</param>
        /// <returns>任务时序流转与耗时画像 DTO</returns>
        Task<TaskTimelineProfilingDto> GetTaskTimelineProfilingAsync(string taskCode);

        /// <summary>
        /// 获取指定时间区间的宏观统计度量报表
        /// 支撑 Dashboard_数据统计(1).xlsx 核心 P0 统计指标
        /// </summary>
        /// <param name="input">查询与过滤条件</param>
        /// <returns>聚合指标统计 DTO</returns>
        Task<TaskExecutionMetricsSummaryDto> GetMetricsSummaryAsync(GetMetricsSummaryInput input);

        /// <summary>
        /// 分页查询细粒度步骤剖析流水列表
        /// </summary>
        /// <param name="input">分页与过滤参数</param>
        /// <returns>步骤明细分页结果</returns>
        Task<PagedResultDto<TaskStepProfilingDto>> GetStepListAsync(GetTaskStepListInput input);
    }
}
