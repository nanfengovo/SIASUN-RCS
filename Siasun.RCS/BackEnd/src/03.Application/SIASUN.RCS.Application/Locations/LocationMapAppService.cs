using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SIASUN.RCS.Interfaces.OperationLogs;
using SIASUN.RCS.Locations.Dtos;
using SIASUN.RCS.Locations.Events;
using SIASUN.RCS.Logs.OperatorLog;
using SIASUN.RCS.Logs.OperatorLogs;
using SIASUN.RCS.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Guids;

namespace SIASUN.RCS.Locations
{
    /// <summary>
    /// 库位与 AGV 地图点位映射管理应用服务实现
    /// 提供点位映射维护、零延迟热重载事件驱动与操作审计日志
    /// </summary>
    [Authorize(RCSPermissions.LocationMap.Default)]
    public class LocationMapAppService : ApplicationService, ILocationMapAppService
    {
        private readonly IRepository<LocationMap, Guid> _locationMapRepository;
        private readonly ILocalEventBus _localEventBus;
        private readonly IOperationLogRecorder _opRecorder;
        private readonly IGuidGenerator _guidGenerator;

        public LocationMapAppService(
            IRepository<LocationMap, Guid> locationMapRepository,
            ILocalEventBus localEventBus,
            IOperationLogRecorder opRecorder,
            IGuidGenerator guidGenerator)
        {
            _locationMapRepository = locationMapRepository;
            _localEventBus = localEventBus;
            _opRecorder = opRecorder;
            _guidGenerator = guidGenerator;
        }

        public async Task<PagedResultDto<LocationMapDto>> GetListAsync(GetLocationMapListInput input)
        {
            var query = await _locationMapRepository.GetQueryableAsync();

            if (!string.IsNullOrWhiteSpace(input.Filter))
            {
                var filter = input.Filter.Trim();
                query = query.Where(x =>
                    x.LocationCode.Contains(filter) ||
                    x.Name.Contains(filter) ||
                    x.StationCode.Contains(filter));
            }

            if (!string.IsNullOrWhiteSpace(input.Area))
            {
                var area = input.Area.Trim();
                query = query.Where(x => x.Area == area);
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

            return new PagedResultDto<LocationMapDto>(
                totalCount,
                items.Select(MapToDto).ToList());
        }

        public async Task<List<LocationMapDto>> GetActiveListAsync()
        {
            var query = await _locationMapRepository.GetQueryableAsync();
            var activeItems = await AsyncExecuter.ToListAsync(
                query.Where(x => x.IsEnabled)
                     .OrderBy(x => x.LocationCode));

            return activeItems.Select(MapToDto).ToList();
        }

        public async Task<LocationMapDto> GetAsync(Guid id)
        {
            var entity = await _locationMapRepository.FindAsync(id);
            if (entity == null)
            {
                throw new EntityNotFoundException(typeof(LocationMap), id);
            }

            return MapToDto(entity);
        }

        public async Task<LocationMapDto?> GetByLocationCodeAsync(string locationCode)
        {
            Check.NotNullOrWhiteSpace(locationCode, nameof(locationCode));

            var entity = await _locationMapRepository.FindAsync(x => x.LocationCode == locationCode);
            return entity == null ? null : MapToDto(entity);
        }

        [Authorize(RCSPermissions.LocationMap.Create)]
        public async Task<LocationMapDto> CreateAsync(CreateLocationMapDto input)
        {
            Check.NotNull(input, nameof(input));

            var existing = await _locationMapRepository.FindAsync(x => x.LocationCode == input.LocationCode);
            if (existing != null)
            {
                throw new UserFriendlyException($"库位编码 [{input.LocationCode}] 的点位映射已存在，不可重复添加");
            }

            var entity = new LocationMap(
                _guidGenerator.Create(),
                input.LocationCode,
                input.Name,
                input.StationCode,
                input.PreDockStationCode,
                input.MapCode,
                input.Heading,
                input.Area,
                input.Description,
                input.IsEnabled);

            await _locationMapRepository.InsertAsync(entity, autoSave: true);

            // 发布领域事件通知内存高速缓存刷新
            await _localEventBus.PublishAsync(new LocationMapChangedEvent(entity.LocationCode, "Created"));

            // 记录调度员操作审计
            _opRecorder.Record(new OperationLogContext
            {
                Module = "Location",
                Action = "CreateLocationMap",
                TargetType = "LocationMap",
                TargetId = entity.LocationCode,
                BeforeState = null,
                AfterState = entity.StationCode,
                Reason = "调度员创建库位点位映射配置",
                Description = $"创建库位 [{entity.LocationCode}] -> 站点 [{entity.StationCode}] 映射"
            }, OperationLogStatus.Success);

            return MapToDto(entity);
        }

        [Authorize(RCSPermissions.LocationMap.Edit)]
        public async Task<LocationMapDto> UpdateAsync(Guid id, UpdateLocationMapDto input)
        {
            Check.NotNull(input, nameof(input));

            var entity = await _locationMapRepository.FindAsync(id);
            if (entity == null)
            {
                throw new EntityNotFoundException(typeof(LocationMap), id);
            }

            var beforeStation = entity.StationCode;
            entity.UpdateDetails(
                input.Name,
                input.StationCode,
                input.PreDockStationCode,
                input.MapCode,
                input.Heading,
                input.Area,
                input.Description);

            if (input.IsEnabled)
            {
                entity.Enable();
            }
            else
            {
                entity.Disable();
            }

            await _locationMapRepository.UpdateAsync(entity, autoSave: true);

            await _localEventBus.PublishAsync(new LocationMapChangedEvent(entity.LocationCode, "Updated"));

            _opRecorder.Record(new OperationLogContext
            {
                Module = "Location",
                Action = "UpdateLocationMap",
                TargetType = "LocationMap",
                TargetId = entity.LocationCode,
                BeforeState = beforeStation,
                AfterState = entity.StationCode,
                Reason = "调度员修改库位点位映射配置",
                Description = $"修改库位 [{entity.LocationCode}] 点位映射，目标站点变更为 [{entity.StationCode}]"
            }, OperationLogStatus.Success);

            return MapToDto(entity);
        }

        [Authorize(RCSPermissions.LocationMap.Delete)]
        public async Task DeleteAsync(Guid id)
        {
            var entity = await _locationMapRepository.FindAsync(id);
            if (entity == null)
            {
                return;
            }

            var locationCode = entity.LocationCode;
            await _locationMapRepository.DeleteAsync(entity, autoSave: true);

            await _localEventBus.PublishAsync(new LocationMapChangedEvent(locationCode, "Deleted"));

            _opRecorder.Record(new OperationLogContext
            {
                Module = "Location",
                Action = "DeleteLocationMap",
                TargetType = "LocationMap",
                TargetId = locationCode,
                BeforeState = entity.StationCode,
                AfterState = null,
                Reason = "调度员删除库位点位映射配置",
                Description = $"删除库位 [{locationCode}] 的点位映射"
            }, OperationLogStatus.Success);
        }

        [Authorize(RCSPermissions.LocationMap.Edit)]
        public async Task RefreshCacheAsync()
        {
            await _localEventBus.PublishAsync(new LocationMapChangedEvent(string.Empty, "CacheRefreshed"));

            _opRecorder.Record(new OperationLogContext
            {
                Module = "Location",
                Action = "RefreshLocationMapCache",
                TargetType = "LocationMap",
                TargetId = "ALL",
                BeforeState = null,
                AfterState = null,
                Reason = "调度员手动触发全量点位缓存热刷新",
                Description = "已发布点位映射热重载事件"
            }, OperationLogStatus.Success);
        }

        private static LocationMapDto MapToDto(LocationMap entity)
        {
            return new LocationMapDto
            {
                Id = entity.Id,
                LocationCode = entity.LocationCode,
                Name = entity.Name,
                StationCode = entity.StationCode,
                PreDockStationCode = entity.PreDockStationCode,
                MapCode = entity.MapCode,
                Heading = entity.Heading,
                Area = entity.Area,
                IsEnabled = entity.IsEnabled,
                Description = entity.Description,
                CreationTime = entity.CreationTime,
                CreatorId = entity.CreatorId,
                LastModificationTime = entity.LastModificationTime,
                LastModifierId = entity.LastModifierId
            };
        }
    }
}
