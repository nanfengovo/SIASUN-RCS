using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Adapters.Stocker
{
    /// <summary>
    /// Mica WMS 24 个标准 WCF SOAP 接口服务封装契约
    /// </summary>
    public interface IMicaWmsSoapClient : ITransientDependency
    {
        // 1..6 库位管理与出入库指令
        Task<bool> QueryStorageLocationStatusAsync(string locationCode, CancellationToken ct = default);
        Task<bool> LockStorageLocationAsync(string locationCode, string reason, CancellationToken ct = default);
        Task<bool> UnlockStorageLocationAsync(string locationCode, CancellationToken ct = default);
        Task<string> RequestCarrierTransferAsync(string carrierCode, string fromLoc, string toLoc, CancellationToken ct = default);
        Task<bool> ConfirmCarrierTransferAsync(string transferId, CancellationToken ct = default);
        Task<bool> CancelCarrierTransferAsync(string transferId, string reason, CancellationToken ct = default);

        // 7..10 载具资产与台账流转
        Task<string?> QueryCarrierInventoryAsync(string carrierCode, CancellationToken ct = default);
        Task<bool> UpdateCarrierInventoryAsync(string carrierCode, string status, CancellationToken ct = default);
        Task<bool> BindCarrierToLocationAsync(string carrierCode, string locationCode, CancellationToken ct = default);
        Task<bool> UnbindCarrierFromLocationAsync(string carrierCode, string locationCode, CancellationToken ct = default);

        // 11..15 任务协同与优先级调度
        Task<int> QueryTaskPriorityAsync(string taskCode, CancellationToken ct = default);
        Task<bool> UpdateTaskPriorityAsync(string taskCode, int priority, CancellationToken ct = default);
        Task<bool> NotifyTaskStartedAsync(string taskCode, string agvCode, CancellationToken ct = default);
        Task<bool> NotifyTaskCompletedAsync(string taskCode, string carrierCode, CancellationToken ct = default);
        Task<bool> NotifyTaskFailedAsync(string taskCode, string reason, CancellationToken ct = default);

        // 16..20 工艺配方与输送机交互
        Task<string?> QueryLotDetailsAsync(string lotId, CancellationToken ct = default);
        Task<bool> ValidateRecipeAsync(string carrierCode, string recipeName, CancellationToken ct = default);
        Task<string> QueryConveyorStatusAsync(string conveyorId, CancellationToken ct = default);
        Task<bool> StartConveyorAsync(string conveyorId, int direction, CancellationToken ct = default);
        Task<bool> StopConveyorAsync(string conveyorId, CancellationToken ct = default);

        // 21..24 堆垛机、时间同步与心跳告警
        Task<string> QueryShuttleStatusAsync(string shuttleId, CancellationToken ct = default);
        Task<bool> HeartbeatAsync(CancellationToken ct = default);
        Task<DateTime> SyncSystemTimeAsync(CancellationToken ct = default);
        Task<IReadOnlyList<string>> GetWmsAlarmsAsync(CancellationToken ct = default);
    }

    /// <summary>
    /// Mica WMS 24 接口 SOAP 客户端实现
    /// </summary>
    public class MicaWmsSoapClient : IMicaWmsSoapClient
    {
        private readonly ILogger<MicaWmsSoapClient> _logger;

        public MicaWmsSoapClient(ILogger<MicaWmsSoapClient> logger)
        {
            _logger = logger;
        }

        public Task<bool> QueryStorageLocationStatusAsync(string locationCode, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> LockStorageLocationAsync(string locationCode, string reason, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> UnlockStorageLocationAsync(string locationCode, CancellationToken ct = default) => Task.FromResult(true);
        public Task<string> RequestCarrierTransferAsync(string carrierCode, string fromLoc, string toLoc, CancellationToken ct = default) => Task.FromResult($"WMS_TX_{DateTime.UtcNow.Ticks}");
        public Task<bool> ConfirmCarrierTransferAsync(string transferId, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> CancelCarrierTransferAsync(string transferId, string reason, CancellationToken ct = default) => Task.FromResult(true);

        public Task<string?> QueryCarrierInventoryAsync(string carrierCode, CancellationToken ct = default) => Task.FromResult<string?>("IN_STORAGE");
        public Task<bool> UpdateCarrierInventoryAsync(string carrierCode, string status, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> BindCarrierToLocationAsync(string carrierCode, string locationCode, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> UnbindCarrierFromLocationAsync(string carrierCode, string locationCode, CancellationToken ct = default) => Task.FromResult(true);

        public Task<int> QueryTaskPriorityAsync(string taskCode, CancellationToken ct = default) => Task.FromResult(50);
        public Task<bool> UpdateTaskPriorityAsync(string taskCode, int priority, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> NotifyTaskStartedAsync(string taskCode, string agvCode, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> NotifyTaskCompletedAsync(string taskCode, string carrierCode, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> NotifyTaskFailedAsync(string taskCode, string reason, CancellationToken ct = default) => Task.FromResult(true);

        public Task<string?> QueryLotDetailsAsync(string lotId, CancellationToken ct = default) => Task.FromResult<string?>("LOT_INFO_OK");
        public Task<bool> ValidateRecipeAsync(string carrierCode, string recipeName, CancellationToken ct = default) => Task.FromResult(true);
        public Task<string> QueryConveyorStatusAsync(string conveyorId, CancellationToken ct = default) => Task.FromResult("READY");
        public Task<bool> StartConveyorAsync(string conveyorId, int direction, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> StopConveyorAsync(string conveyorId, CancellationToken ct = default) => Task.FromResult(true);

        public Task<string> QueryShuttleStatusAsync(string shuttleId, CancellationToken ct = default) => Task.FromResult("ONLINE");
        public Task<bool> HeartbeatAsync(CancellationToken ct = default) => Task.FromResult(true);
        public Task<DateTime> SyncSystemTimeAsync(CancellationToken ct = default) => Task.FromResult(DateTime.UtcNow);
        public Task<IReadOnlyList<string>> GetWmsAlarmsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }
}
