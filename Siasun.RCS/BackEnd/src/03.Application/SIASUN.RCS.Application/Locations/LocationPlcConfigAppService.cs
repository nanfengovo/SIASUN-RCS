using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SIASUN.RCS.Interfaces.OperationLogs;
using SIASUN.RCS.Locations.Dtos;
using SIASUN.RCS.Logs.OperatorLog;
using SIASUN.RCS.Logs.OperatorLogs;
using SIASUN.RCS.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace SIASUN.RCS.Locations
{
    /// <summary>
    /// 库位 PLC 硬件安全联锁配置应用服务实现
    /// </summary>
    [Authorize(RCSPermissions.LocationPlcConfig.Default)]
    public class LocationPlcConfigAppService : ApplicationService, ILocationPlcConfigAppService
    {
        private readonly IRepository<LocationPlcConfig, Guid> _repository;
        private readonly IOperationLogRecorder _opRecorder;
        private readonly IGuidGenerator _guidGenerator;

        public LocationPlcConfigAppService(
            IRepository<LocationPlcConfig, Guid> repository,
            IOperationLogRecorder opRecorder,
            IGuidGenerator guidGenerator)
        {
            _repository = repository;
            _opRecorder = opRecorder;
            _guidGenerator = guidGenerator;
        }

        public async Task<PagedResultDto<LocationPlcConfigDto>> GetListAsync(GetLocationPlcConfigListInput input)
        {
            var query = await _repository.GetQueryableAsync();

            if (!string.IsNullOrWhiteSpace(input.Filter))
            {
                var filter = input.Filter.Trim();
                query = query.Where(x => x.LocationCode.Contains(filter) || (x.GatewayId != null && x.GatewayId.Contains(filter)));
            }

            if (input.IsEnabled.HasValue)
            {
                query = query.Where(x => x.IsEnabled == input.IsEnabled.Value);
            }

            var totalCount = await AsyncExecuter.CountAsync(query);

            var items = await AsyncExecuter.ToListAsync(
                query.OrderBy(x => x.LocationCode)
                     .Skip(input.SkipCount)
                     .Take(input.MaxResultCount));

            return new PagedResultDto<LocationPlcConfigDto>(
                totalCount,
                items.Select(MapToDto).ToList());
        }

        public async Task<LocationPlcConfigDto> GetAsync(Guid id)
        {
            var entity = await _repository.FindAsync(id);
            if (entity == null)
            {
                throw new EntityNotFoundException(typeof(LocationPlcConfig), id);
            }

            return MapToDto(entity);
        }

        public async Task<LocationPlcConfigDto?> GetByLocationCodeAsync(string locationCode)
        {
            Check.NotNullOrWhiteSpace(locationCode, nameof(locationCode));

            var entity = await _repository.FindAsync(x => x.LocationCode == locationCode);
            return entity == null ? null : MapToDto(entity);
        }

        [Authorize(RCSPermissions.LocationPlcConfig.Create)]
        public async Task<LocationPlcConfigDto> CreateAsync(CreateLocationPlcConfigDto input)
        {
            Check.NotNull(input, nameof(input));

            var existing = await _repository.FindAsync(x => x.LocationCode == input.LocationCode);
            if (existing != null)
            {
                throw new UserFriendlyException($"库位 [{input.LocationCode}] 的 PLC 硬件联锁配置已存在，不可重复添加");
            }

            var entity = new LocationPlcConfig(
                _guidGenerator.Create(),
                input.LocationCode,
                input.IsEnabled,
                input.GatewayId,
                input.MaterialPresenceTag,
                input.InterlockReadyTag,
                input.TimeoutSeconds,
                input.Description);

            await _repository.InsertAsync(entity, autoSave: true);

            _opRecorder.Record(new OperationLogContext
            {
                Module = "Location",
                Action = "CreateLocationPlcConfig",
                TargetType = "LocationPlcConfig",
                TargetId = entity.LocationCode,
                Reason = "调度员配置库位硬件 PLC 联锁点表",
                Description = $"创建库位 [{entity.LocationCode}] 关联 PLC 网关 [{entity.GatewayId}]"
            }, OperationLogStatus.Success);

            return MapToDto(entity);
        }

        [Authorize(RCSPermissions.LocationPlcConfig.Edit)]
        public async Task<LocationPlcConfigDto> UpdateAsync(Guid id, UpdateLocationPlcConfigDto input)
        {
            Check.NotNull(input, nameof(input));

            var entity = await _repository.FindAsync(id);
            if (entity == null)
            {
                throw new EntityNotFoundException(typeof(LocationPlcConfig), id);
            }

            entity.Update(
                input.IsEnabled,
                input.GatewayId,
                input.MaterialPresenceTag,
                input.InterlockReadyTag,
                input.TimeoutSeconds,
                input.Description);

            await _repository.UpdateAsync(entity, autoSave: true);

            _opRecorder.Record(new OperationLogContext
            {
                Module = "Location",
                Action = "UpdateLocationPlcConfig",
                TargetType = "LocationPlcConfig",
                TargetId = entity.LocationCode,
                Reason = "调度员修改库位硬件 PLC 联锁点表",
                Description = $"修改库位 [{entity.LocationCode}] 硬件联锁参数"
            }, OperationLogStatus.Success);

            return MapToDto(entity);
        }

        [Authorize(RCSPermissions.LocationPlcConfig.Delete)]
        public async Task DeleteAsync(Guid id)
        {
            var entity = await _repository.FindAsync(id);
            if (entity == null)
            {
                return;
            }

            var code = entity.LocationCode;
            await _repository.DeleteAsync(entity, autoSave: true);

            _opRecorder.Record(new OperationLogContext
            {
                Module = "Location",
                Action = "DeleteLocationPlcConfig",
                TargetType = "LocationPlcConfig",
                TargetId = code,
                Reason = "调度员删除库位硬件 PLC 联锁点表",
                Description = $"删除库位 [{code}] 的硬件联锁配置"
            }, OperationLogStatus.Success);
        }

        private static LocationPlcConfigDto MapToDto(LocationPlcConfig entity)
        {
            return new LocationPlcConfigDto
            {
                Id = entity.Id,
                LocationCode = entity.LocationCode,
                IsEnabled = entity.IsEnabled,
                GatewayId = entity.GatewayId,
                MaterialPresenceTag = entity.MaterialPresenceTag,
                InterlockReadyTag = entity.InterlockReadyTag,
                TimeoutSeconds = entity.TimeoutSeconds,
                Description = entity.Description,
                CreationTime = entity.CreationTime,
                CreatorId = entity.CreatorId,
                LastModificationTime = entity.LastModificationTime,
                LastModifierId = entity.LastModifierId
            };
        }
    }
}
