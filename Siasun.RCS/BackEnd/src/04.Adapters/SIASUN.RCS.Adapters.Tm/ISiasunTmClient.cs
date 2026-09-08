using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Adapters.Tm
{
    /// <summary>
    /// 新松 TM (Traffic Manager) 交通管理器标准 12 端点驱动客户端契约
    /// </summary>
    public interface ISiasunTmClient : ITransientDependency
    {
        /// <summary>
        /// 1. 派发程段搬运任务 (POST /api/tm/task/dispatch)
        /// </summary>
        Task<TmResponse<string>> DispatchTaskAsync(TmDispatchTaskRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// 2. 取消车载任务 (POST /api/tm/task/cancel)
        /// </summary>
        Task<TmResponse<bool>> CancelTaskAsync(string agvCode, string taskCode, string reason, CancellationToken cancellationToken = default);

        /// <summary>
        /// 3. 暂停任务 (POST /api/tm/task/pause)
        /// </summary>
        Task<TmResponse<bool>> PauseTaskAsync(string agvCode, string taskCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// 4. 恢复任务 (POST /api/tm/task/resume)
        /// </summary>
        Task<TmResponse<bool>> ResumeTaskAsync(string agvCode, string taskCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// 5. 获取任务状态 (GET /api/tm/task/status/{taskCode})
        /// </summary>
        Task<TmResponse<TmTaskStatusDto>> GetTaskStatusAsync(string taskCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// 6. 查询单车实时状态 (GET /api/tm/agv/status/{agvCode})
        /// </summary>
        Task<TmResponse<TmAgvStatusDto>> QueryAgvStatusAsync(string agvCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// 7. 查询全车队状态列表 (GET /api/tm/agv/list)
        /// </summary>
        Task<TmResponse<IReadOnlyList<TmAgvStatusDto>>> QueryAgvListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 8. 绑定载具与车辆 (POST /api/tm/agv/carrier/bind)
        /// </summary>
        Task<TmResponse<bool>> BindCarrierAsync(string agvCode, string carrierCode, int slotIndex = 1, CancellationToken cancellationToken = default);

        /// <summary>
        /// 9. 解绑载具与车辆 (POST /api/tm/agv/carrier/unbind)
        /// </summary>
        Task<TmResponse<bool>> UnbindCarrierAsync(string agvCode, string carrierCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// 10. 设置路段交通管制优先级 (POST /api/tm/traffic/priority)
        /// </summary>
        Task<TmResponse<bool>> SetPathPriorityAsync(string sectionCode, int priority, CancellationToken cancellationToken = default);

        /// <summary>
        /// 11. 清除车体报警 (POST /api/tm/agv/alarm/clear)
        /// </summary>
        Task<TmResponse<bool>> ClearAlarmAsync(string agvCode, string alarmCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// 12. 区域或全场紧急停止 (POST /api/tm/traffic/estop)
        /// </summary>
        Task<TmResponse<bool>> EmergencyStopAsync(string areaCode, bool enable, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// TM 统一响应封装
    /// </summary>
    public record TmResponse<T>(
        bool Success,
        int Code,
        string Message,
        T? Data);

    /// <summary>
    /// TM 任务派发请求模型
    /// </summary>
    public record TmDispatchTaskRequest(
        string TaskCode,
        string AgvCode,
        string Leg,
        string OptionCode,
        string TargetStation,
        string? TraceId = null);

    /// <summary>
    /// TM 任务状态模型
    /// </summary>
    public record TmTaskStatusDto(
        string TaskCode,
        string AgvCode,
        string State,
        string CurrentStation,
        int ProgressPercent);

    /// <summary>
    /// TM 车辆实时遥测状态模型
    /// </summary>
    public record TmAgvStatusDto(
        string AgvCode,
        int BatteryLevel,
        string CurrentStation,
        string OperationalState,
        bool HasAlarm,
        string? CurrentCarrier);
}
