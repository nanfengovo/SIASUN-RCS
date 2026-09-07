using System.Collections.Generic;
using System.Threading.Tasks;
using SIASUN.RCS.Locations.Dtos;
using Volo.Abp.Application.Services;

namespace SIASUN.RCS.Locations
{
    /// <summary>
    /// 库位锁与人工运维管理应用服务契约
    /// 提供调度大屏、数字孪生与调度员人工干预所需的库位锁定状态展示与管理
    /// </summary>
    public interface ILocationLockAppService : IApplicationService
    {
        /// <summary>
        /// 获取当前所有处于活跃锁定状态的库位列表（支持大屏投影）
        /// </summary>
        Task<List<LocationLockDto>> GetActiveLocksAsync();

        /// <summary>
        /// 获取指定库位的锁定详情
        /// </summary>
        /// <param name="locationCode">库位编码</param>
        Task<LocationLockDto?> GetLockAsync(string locationCode);

        /// <summary>
        /// 调度员强制解除库位锁定
        /// </summary>
        /// <param name="input">强制解锁参数</param>
        Task<LocationLockOperationResultDto> ForceUnlockAsync(ForceUnlockLocationInput input);

        /// <summary>
        /// 调度员人工维护封锁库位
        /// </summary>
        /// <param name="input">维护封锁参数</param>
        Task<LocationLockOperationResultDto> LockForMaintenanceAsync(LockLocationForMaintenanceInput input);

        /// <summary>
        /// 调度员解除维护封锁
        /// </summary>
        /// <param name="input">解封参数</param>
        Task<LocationLockOperationResultDto> UnlockMaintenanceAsync(UnlockLocationMaintenanceInput input);
    }

    /// <summary>
    /// 强制解锁入参
    /// </summary>
    public class ForceUnlockLocationInput
    {
        public string LocationCode { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 维护封锁入参
    /// </summary>
    public class LockLocationForMaintenanceInput
    {
        public string LocationCode { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 解除维护入参
    /// </summary>
    public class UnlockLocationMaintenanceInput
    {
        public string LocationCode { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
