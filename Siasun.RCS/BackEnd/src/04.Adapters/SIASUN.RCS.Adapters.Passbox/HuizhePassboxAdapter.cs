using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Ports;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Adapters.Passbox
{
    /// <summary>
    /// 晖哲 8 接口双门互锁传递窗状态机适配器（六边形架构出站适配器）
    /// 遵循《AGENTS.md》铁律 4：封装半导体高洁净度传递窗双门互斥、自净除尘吹扫与气闸控制状态机
    /// 涵盖 8 个工控交互接口：
    /// 1. RequestOpenFrontDoorAsync: 申请开启外侧前门
    /// 2. ReportFrontDoorOpenedAsync: 前门开启就绪变位上报
    /// 3. CloseFrontDoorAsync: 关闭外侧前门并机械锁紧
    /// 4. StartCleanroomPurgeAsync: 启动自净风淋吹扫除尘
    /// 5. ReportPurgeCompletedAsync: 风淋吹扫周期完成上报
    /// 6. RequestOpenBackDoorAsync: 申请开启内侧洁净室后门
    /// 7. ReportBackDoorOpenedAsync: 后门开启就绪变位上报
    /// 8. NotifyExitedAndResetAsync: 车辆驶离、关门并复位互锁状态机
    /// </summary>
    public class HuizhePassboxAdapter : IPassboxAdapter, ITransientDependency
    {
        private readonly ILogger<HuizhePassboxAdapter> _logger;

        /// <summary>
        /// 传递窗当前互锁状态机字典: Key 为 PassboxId, Value 为当前状态
        /// </summary>
        private static readonly ConcurrentDictionary<string, PassboxState> _passboxStates = new(StringComparer.OrdinalIgnoreCase);

        public HuizhePassboxAdapter(ILogger<HuizhePassboxAdapter> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 接口 1: 申请开启前门（外侧非洁净侧）
        /// </summary>
        public Task<PassboxOperationResult> RequestOpenFrontDoorAsync(
            string passboxId,
            string agvCode,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("晖哲传递窗 [接口1: 申请开前门]: Passbox={Passbox}, AGV={Agv}", passboxId, agvCode);

            var state = _passboxStates.GetOrAdd(passboxId, _ => PassboxState.Idle);
            if (state != PassboxState.Idle && state != PassboxState.Closed)
            {
                _logger.LogWarning("晖哲传递窗 [{Passbox}] 当前处于非空闲状态 [{State}]，互锁拒绝开门申请", passboxId, state);
                return Task.FromResult(new PassboxOperationResult(false, state.ToString(), "对侧门开启或正处于吹扫中，互锁阻断"));
            }

            _passboxStates[passboxId] = PassboxState.FrontDoorOpening;
            _logger.LogInformation("晖哲传递窗 [{Passbox}] 前门开门动作已触发", passboxId);
            return Task.FromResult(new PassboxOperationResult(true, PassboxState.FrontDoorOpening.ToString()));
        }

        /// <summary>
        /// 接口 2: 前门已完全开启就绪反馈
        /// </summary>
        public Task<bool> ReportFrontDoorOpenedAsync(string passboxId, string agvCode, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("晖哲传递窗 [接口2: 前门已开启就绪]: Passbox={Passbox}, AGV={Agv}", passboxId, agvCode);
            _passboxStates[passboxId] = PassboxState.FrontDoorOpened;
            return Task.FromResult(true);
        }

        /// <summary>
        /// 接口 3: 请求关闭前门并锁紧
        /// </summary>
        public Task<bool> CloseFrontDoorAsync(string passboxId, string agvCode, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("晖哲传递窗 [接口3: 请求关前门]: Passbox={Passbox}, AGV={Agv}", passboxId, agvCode);
            _passboxStates[passboxId] = PassboxState.FrontDoorClosing;
            return Task.FromResult(true);
        }

        /// <summary>
        /// 接口 4: 启动风淋自净吹扫除尘
        /// </summary>
        public Task<bool> StartCleanroomPurgeAsync(string passboxId, string agvCode, int durationSeconds = 15, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("晖哲传递窗 [接口4: 启动洁净吹扫]: Passbox={Passbox}, AGV={Agv}, 设定吹扫时长={Duration}s",
                passboxId, agvCode, durationSeconds);
            _passboxStates[passboxId] = PassboxState.Purging;
            return Task.FromResult(true);
        }

        /// <summary>
        /// 接口 5: 吹扫完成信号上报
        /// </summary>
        public Task<bool> ReportPurgeCompletedAsync(string passboxId, string agvCode, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("晖哲传递窗 [接口5: 吹扫完成]: Passbox={Passbox}, AGV={Agv}", passboxId, agvCode);
            _passboxStates[passboxId] = PassboxState.Purged;
            return Task.FromResult(true);
        }

        /// <summary>
        /// 接口 6: 申请开启内侧洁净室后门
        /// </summary>
        public Task<PassboxOperationResult> RequestOpenBackDoorAsync(
            string passboxId,
            string agvCode,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("晖哲传递窗 [接口6: 申请开后门]: Passbox={Passbox}, AGV={Agv}", passboxId, agvCode);

            _passboxStates[passboxId] = PassboxState.BackDoorOpening;
            return Task.FromResult(new PassboxOperationResult(true, PassboxState.BackDoorOpening.ToString()));
        }

        /// <summary>
        /// 接口 7: 后门已完全开启就绪反馈
        /// </summary>
        public Task<bool> ReportBackDoorOpenedAsync(string passboxId, string agvCode, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("晖哲传递窗 [接口7: 后门已开启就绪]: Passbox={Passbox}, AGV={Agv}", passboxId, agvCode);
            _passboxStates[passboxId] = PassboxState.BackDoorOpened;
            return Task.FromResult(true);
        }

        /// <summary>
        /// 接口 8: 车辆完全驶离后，关门并复位互锁状态机
        /// </summary>
        public Task<PassboxOperationResult> NotifyExitedAndResetAsync(
            string passboxId,
            string agvCode,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("晖哲传递窗 [接口8: 车辆驶离并复位]: Passbox={Passbox}, AGV={Agv}", passboxId, agvCode);

            _passboxStates[passboxId] = PassboxState.Idle;
            return Task.FromResult(new PassboxOperationResult(true, PassboxState.Idle.ToString()));
        }

        /// <inheritdoc />
        public async Task<PassboxOperationResult> CloseAndStartPurgeAsync(
            string passboxId,
            string agvCode,
            CancellationToken cancellationToken = default)
        {
            await CloseFrontDoorAsync(passboxId, agvCode, cancellationToken);
            await StartCleanroomPurgeAsync(passboxId, agvCode, 15, cancellationToken);
            return new PassboxOperationResult(true, PassboxState.Purging.ToString());
        }
    }

    /// <summary>
    /// 传递窗双门互锁状态枚举
    /// </summary>
    public enum PassboxState
    {
        Idle = 0,
        Closed = 1,
        FrontDoorOpening = 2,
        FrontDoorOpened = 3,
        FrontDoorClosing = 4,
        Purging = 5,
        Purged = 6,
        BackDoorOpening = 7,
        BackDoorOpened = 8,
        BackDoorClosing = 9
    }
}
