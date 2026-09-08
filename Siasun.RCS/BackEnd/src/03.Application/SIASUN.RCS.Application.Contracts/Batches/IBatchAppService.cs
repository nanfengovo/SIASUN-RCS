using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SIASUN.RCS.Batches.Dtos;
using Volo.Abp.Application.Services;

namespace SIASUN.RCS.Batches
{
    /// <summary>
    /// AGV 批次管理与多车编排应用服务契约
    /// </summary>
    public interface IBatchAppService : IApplicationService
    {
        /// <summary>
        /// 创建并编排新搬运批次订单（自动拆解为单车搬运子任务）
        /// </summary>
        /// <param name="input">批次创建参数</param>
        /// <returns>已创建的批次信息与生成的子任务编号列表</returns>
        Task<AgvBatchDto> CreateAndDecomposeBatchAsync(CreateBatchDto input);

        /// <summary>
        /// 查询指定批次状态与进度
        /// </summary>
        /// <param name="id">批次 ID</param>
        /// <returns>批次详情</returns>
        Task<AgvBatchDto> GetAsync(Guid id);

        /// <summary>
        /// 人工取消指定批次
        /// </summary>
        /// <param name="id">批次 ID</param>
        /// <param name="reason">取消原因</param>
        Task CancelAsync(Guid id, string reason);
    }
}
