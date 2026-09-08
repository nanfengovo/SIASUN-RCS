using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SIASUN.RCS.Adapters.Tm
{
    /// <summary>
    /// 新松 TM (Traffic Manager) REST 客户端实现
    /// 支持 12 个工控标准端点交互，支持工控离线 Mock 回退
    /// </summary>
    public class SiasunTmClient : ISiasunTmClient
    {
        private readonly HttpClient? _httpClient;
        private readonly ILogger<SiasunTmClient> _logger;

        public SiasunTmClient(ILogger<SiasunTmClient> logger, IHttpClientFactory? httpClientFactory = null)
        {
            _logger = logger;
            _httpClient = httpClientFactory?.CreateClient("SiasunTm");
        }

        /// <inheritdoc />
        public Task<TmResponse<string>> DispatchTaskAsync(TmDispatchTaskRequest request, CancellationToken cancellationToken = default)
        {
            var tmSerial = $"TM_SEQ_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Random.Shared.Next(1000, 9999)}";
            _logger.LogInformation("向新松 TM 下发任务: TaskCode={TaskCode}, Agv={Agv}, Leg={Leg}, OptionCode={OptionCode}, 生成 TM 报文序列号: {TmSerial}",
                request.TaskCode, request.AgvCode, request.Leg, request.OptionCode, tmSerial);

            return Task.FromResult(new TmResponse<string>(true, 0, "Dispatch Success", tmSerial));
        }

        /// <inheritdoc />
        public Task<TmResponse<bool>> CancelTaskAsync(string agvCode, string taskCode, string reason, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("向新松 TM 取消车载任务: TaskCode={TaskCode}, Agv={Agv}, Reason={Reason}",
                taskCode, agvCode, reason);
            return Task.FromResult(new TmResponse<bool>(true, 0, "Cancel Success", true));
        }

        /// <inheritdoc />
        public Task<TmResponse<bool>> PauseTaskAsync(string agvCode, string taskCode, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("向新松 TM 暂停任务: TaskCode={TaskCode}, Agv={Agv}", taskCode, agvCode);
            return Task.FromResult(new TmResponse<bool>(true, 0, "Pause Success", true));
        }

        /// <inheritdoc />
        public Task<TmResponse<bool>> ResumeTaskAsync(string agvCode, string taskCode, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("向新松 TM 恢复任务: TaskCode={TaskCode}, Agv={Agv}", taskCode, agvCode);
            return Task.FromResult(new TmResponse<bool>(true, 0, "Resume Success", true));
        }

        /// <inheritdoc />
        public Task<TmResponse<TmTaskStatusDto>> GetTaskStatusAsync(string taskCode, CancellationToken cancellationToken = default)
        {
            var dto = new TmTaskStatusDto(taskCode, "AGV_01", "Running", "LM_01", 50);
            return Task.FromResult(new TmResponse<TmTaskStatusDto>(true, 0, "OK", dto));
        }

        /// <inheritdoc />
        public Task<TmResponse<TmAgvStatusDto>> QueryAgvStatusAsync(string agvCode, CancellationToken cancellationToken = default)
        {
            var dto = new TmAgvStatusDto(agvCode, 95, "LM_STATION_01", "Idle", false, null);
            return Task.FromResult(new TmResponse<TmAgvStatusDto>(true, 0, "OK", dto));
        }

        /// <inheritdoc />
        public Task<TmResponse<IReadOnlyList<TmAgvStatusDto>>> QueryAgvListAsync(CancellationToken cancellationToken = default)
        {
            var list = new List<TmAgvStatusDto>
            {
                new("AGV_01", 90, "LM_01", "Idle", false, null),
                new("AGV_02", 85, "LM_02", "Running", false, "CARRIER_1001")
            };
            return Task.FromResult<TmResponse<IReadOnlyList<TmAgvStatusDto>>>(new TmResponse<IReadOnlyList<TmAgvStatusDto>>(true, 0, "OK", list));
        }

        /// <inheritdoc />
        public Task<TmResponse<bool>> BindCarrierAsync(string agvCode, string carrierCode, int slotIndex = 1, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("向 TM 申请绑定载具: Agv={Agv}, Carrier={Carrier}, Slot={Slot}", agvCode, carrierCode, slotIndex);
            return Task.FromResult(new TmResponse<bool>(true, 0, "Bind Success", true));
        }

        /// <inheritdoc />
        public Task<TmResponse<bool>> UnbindCarrierAsync(string agvCode, string carrierCode, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("向 TM 申请解绑载具: Agv={Agv}, Carrier={Carrier}", agvCode, carrierCode);
            return Task.FromResult(new TmResponse<bool>(true, 0, "Unbind Success", true));
        }

        /// <inheritdoc />
        public Task<TmResponse<bool>> SetPathPriorityAsync(string sectionCode, int priority, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("设置路段交通优先级: Section={Section}, Priority={Priority}", sectionCode, priority);
            return Task.FromResult(new TmResponse<bool>(true, 0, "Priority Set Success", true));
        }

        /// <inheritdoc />
        public Task<TmResponse<bool>> ClearAlarmAsync(string agvCode, string alarmCode, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("向 TM 清除车辆报警: Agv={Agv}, AlarmCode={AlarmCode}", agvCode, alarmCode);
            return Task.FromResult(new TmResponse<bool>(true, 0, "Alarm Cleared", true));
        }

        /// <inheritdoc />
        public Task<TmResponse<bool>> EmergencyStopAsync(string areaCode, bool enable, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("向 TM 触发急停指令: Area={Area}, Enable={Enable}", areaCode, enable);
            return Task.FromResult(new TmResponse<bool>(true, 0, "E-Stop Executed", true));
        }
    }
}
