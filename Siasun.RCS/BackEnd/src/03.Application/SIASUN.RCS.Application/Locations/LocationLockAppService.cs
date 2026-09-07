using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using SIASUN.RCS.Locations.Commands;
using SIASUN.RCS.Locations.Dtos;
using SIASUN.RCS.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace SIASUN.RCS.Locations
{
    /// <summary>
    /// 库位锁与人工运维管理应用服务实现（极薄 API 门面）
    /// 承载现场调度大屏展示与调度员人工干预，通过 CQRS 管道自动生成不可抵赖的操作审计
    /// </summary>
    [Authorize(RCSPermissions.LocationLock.Default)]
    public class LocationLockAppService : ApplicationService, ILocationLockAppService
    {
        private readonly IMediator _mediator;
        private readonly IRepository<LocationLock, Guid> _lockRepository;

        public LocationLockAppService(
            IMediator mediator,
            IRepository<LocationLock, Guid> lockRepository)
        {
            _mediator = mediator;
            _lockRepository = lockRepository;
        }

        public async Task<List<LocationLockDto>> GetActiveLocksAsync()
        {
            var locks = await _lockRepository.GetListAsync();

            return locks.Select(l => new LocationLockDto
            {
                LocationCode = l.LocationCode,
                LockType = l.LockType,
                TaskId = l.TaskId,
                VehicleCode = l.VehicleCode,
                CreationTime = l.CreationTime,
                LeaseExpirationTime = l.LeaseExpirationTime,
                Reason = l.Reason
            }).ToList();
        }

        public async Task<LocationLockDto?> GetLockAsync(string locationCode)
        {
            Check.NotNullOrWhiteSpace(locationCode, nameof(locationCode));

            var lockItem = await _lockRepository.FirstOrDefaultAsync(l => l.LocationCode == locationCode);
            if (lockItem == null) return null;

            return new LocationLockDto
            {
                LocationCode = lockItem.LocationCode,
                LockType = lockItem.LockType,
                TaskId = lockItem.TaskId,
                VehicleCode = lockItem.VehicleCode,
                CreationTime = lockItem.CreationTime,
                LeaseExpirationTime = lockItem.LeaseExpirationTime,
                Reason = lockItem.Reason
            };
        }

        [Authorize(RCSPermissions.LocationLock.ForceUnlock)]
        public async Task<LocationLockOperationResultDto> ForceUnlockAsync(ForceUnlockLocationInput input)
        {
            Check.NotNull(input, nameof(input));
            return await _mediator.Send(new ForceUnlockLocationCommand(input.LocationCode, input.Reason));
        }

        [Authorize(RCSPermissions.LocationLock.Maintenance)]
        public async Task<LocationLockOperationResultDto> LockForMaintenanceAsync(LockLocationForMaintenanceInput input)
        {
            Check.NotNull(input, nameof(input));
            return await _mediator.Send(new LockLocationForMaintenanceCommand(input.LocationCode, input.Reason));
        }

        [Authorize(RCSPermissions.LocationLock.Maintenance)]
        public async Task<LocationLockOperationResultDto> UnlockMaintenanceAsync(UnlockLocationMaintenanceInput input)
        {
            Check.NotNull(input, nameof(input));
            return await _mediator.Send(new UnlockLocationMaintenanceCommand(input.LocationCode, input.Reason));
        }
    }
}
