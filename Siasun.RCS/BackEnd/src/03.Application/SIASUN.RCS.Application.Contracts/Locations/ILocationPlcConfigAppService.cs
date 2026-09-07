using System;
using System.Threading.Tasks;
using SIASUN.RCS.Locations.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SIASUN.RCS.Locations
{
    /// <summary>
    /// 库位 PLC 硬件联锁配置管理应用服务契约
    /// </summary>
    public interface ILocationPlcConfigAppService : IApplicationService
    {
        /// <summary>
        /// 分页获取库位 PLC 联锁配置列表
        /// </summary>
        Task<PagedResultDto<LocationPlcConfigDto>> GetListAsync(GetLocationPlcConfigListInput input);

        /// <summary>
        /// 根据主键获取单个配置
        /// </summary>
        Task<LocationPlcConfigDto> GetAsync(Guid id);

        /// <summary>
        /// 根据库位编码获取 PLC 联锁配置
        /// </summary>
        Task<LocationPlcConfigDto?> GetByLocationCodeAsync(string locationCode);

        /// <summary>
        /// 创建库位 PLC 联锁配置
        /// </summary>
        Task<LocationPlcConfigDto> CreateAsync(CreateLocationPlcConfigDto input);

        /// <summary>
        /// 更新库位 PLC 联锁配置
        /// </summary>
        Task<LocationPlcConfigDto> UpdateAsync(Guid id, UpdateLocationPlcConfigDto input);

        /// <summary>
        /// 删除库位 PLC 联锁配置
        /// </summary>
        Task DeleteAsync(Guid id);
    }
}
