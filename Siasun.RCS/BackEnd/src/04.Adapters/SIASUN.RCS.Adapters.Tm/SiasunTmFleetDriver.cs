using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Ports;
using SIASUN.RCS.Tasks;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Adapters.Tm
{
    /// <summary>
    /// 新松移动机器人车队 TM 驱动适配器（六边形架构出站适配器）
    /// 遵循《AGENTS.md》铁律 3：统一通过 TaskSerialRegistry 维护底层 TM 报文序列号与内部任务映射，严禁字符串替换 hack
    /// </summary>
    public class SiasunTmFleetDriver : IAgvFleetDriver, ITransientDependency
    {
        private readonly ISiasunTmClient _tmClient;
        private readonly ITaskSerialRegistry _taskSerialRegistry;
        private readonly ILogger<SiasunTmFleetDriver> _logger;

        /// <inheritdoc />
        public string ProtocolName => "SIASUN_TM";

        public SiasunTmFleetDriver(
            ISiasunTmClient tmClient,
            ITaskSerialRegistry taskSerialRegistry,
            ILogger<SiasunTmFleetDriver> logger)
        {
            _tmClient = tmClient;
            _taskSerialRegistry = taskSerialRegistry;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<FleetDispatchResult> DispatchLegAsync(
            string taskCode,
            string agvCode,
            string activeLeg,
            string optionCode,
            string targetStation,
            string? traceId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("正在通过 SIASUN_TM 协议下发搬运程段: TaskCode={TaskCode}, AGV={AgvCode}, Leg={Leg}, OptionCode={OptionCode}, Target={TargetStation}, TraceId={TraceId}",
                    taskCode, agvCode, activeLeg, optionCode, targetStation, traceId);

                var request = new TmDispatchTaskRequest(
                    TaskCode: taskCode,
                    AgvCode: agvCode,
                    Leg: activeLeg,
                    OptionCode: optionCode,
                    TargetStation: targetStation,
                    TraceId: traceId);

                var response = await _tmClient.DispatchTaskAsync(request, cancellationToken);

                if (!response.Success || string.IsNullOrWhiteSpace(response.Data))
                {
                    _logger.LogError("TM 派发指令失败: Code={Code}, Message={Message}", response.Code, response.Message);
                    return new FleetDispatchResult(false, null, response.Message);
                }

                var tmSerial = response.Data;

                // 统一登记底层序列号与任务的双向映射
                await _taskSerialRegistry.RegisterAsync(
                    taskId: Guid.Empty, // 若无 Guid 则以 taskCode 为准，或由上层传入映射
                    taskCode: taskCode,
                    tmSerial: tmSerial,
                    leg: activeLeg,
                    stepIndex: 1,
                    waitingEvent: $"TM_LEG_{activeLeg}_COMPLETED",
                    vehicleCode: agvCode,
                    cancellationToken: cancellationToken);

                _logger.LogInformation("TM 派发成功并登记 TaskSerialRegistry: TaskCode={TaskCode}, TmSerial={TmSerial}",
                    taskCode, tmSerial);

                return new FleetDispatchResult(true, tmSerial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "向 TM 下发任务发生通信异常: TaskCode={TaskCode}, AGV={AgvCode}", taskCode, agvCode);
                return new FleetDispatchResult(false, null, ex.Message);
            }
        }

        /// <inheritdoc />
        public async Task<bool> CancelMissionAsync(
            string agvCode,
            string taskCode,
            string reason,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogWarning("正在通过 SIASUN_TM 协议取消任务: AGV={AgvCode}, TaskCode={TaskCode}, 原因: {Reason}",
                    agvCode, taskCode, reason);

                var response = await _tmClient.CancelTaskAsync(agvCode, taskCode, reason, cancellationToken);
                return response.Success && response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "向 TM 发送取消指令异常: AGV={AgvCode}, TaskCode={TaskCode}", agvCode, taskCode);
                return false;
            }
        }
    }
}
