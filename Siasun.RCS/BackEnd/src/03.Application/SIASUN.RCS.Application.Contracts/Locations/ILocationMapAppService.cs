using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SIASUN.RCS.Locations.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SIASUN.RCS.Locations
{
    /// <summary>
    /// 库位与 AGV 地图点位映射管理服务契约
    /// </summary>
    public interface ILocationMapAppService : IApplicationService
    {
        /// <summary>
        /// 分页获取点位映射列表
        /// </summary>
        /// <param name="input">分页与过滤参数</param>
        /// <returns>分页映射结果</returns>
        Task<PagedResultDto<LocationMapDto>> GetListAsync(GetLocationMapListInput input);

        /// <summary>
        /// 获取所有已启用的点位映射列表（用于前端大屏与调度监控渲染）
        /// </summary>
        /// <returns>启用的点位映射列表</returns>
        Task<List<LocationMapDto>> GetActiveListAsync();

        /// <summary>
        /// 根据主键获取单个点位映射
        /// </summary>
        /// <param name="id">映射主键</param>
        /// <returns>点位映射 DTO</returns>
        Task<LocationMapDto> GetAsync(Guid id);

        /// <summary>
        /// 根据业务库位编码获取点位映射
        /// </summary>
        /// <param name="locationCode">业务库位编码</param>
        /// <returns>点位映射 DTO，若不存在返回 null</returns>
        Task<LocationMapDto?> GetByLocationCodeAsync(string locationCode);

        /// <summary>
        /// 创建点位映射
        /// </summary>
        /// <param name="input">创建参数</param>
        /// <returns>创建后的映射 DTO</returns>
        Task<LocationMapDto> CreateAsync(CreateLocationMapDto input);

        /// <summary>
        /// 更新点位映射
        /// </summary>
        /// <param name="id">映射主键</param>
        /// <param name="input">更新参数</param>
        /// <returns>更新后的映射 DTO</returns>
        Task<LocationMapDto> UpdateAsync(Guid id, UpdateLocationMapDto input);

        /// <summary>
        /// 删除点位映射
        /// </summary>
        /// <param name="id">映射主键</param>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// 手动主动刷新调度微内核点位高速缓存
        /// </summary>
        Task RefreshCacheAsync();
    }
}
