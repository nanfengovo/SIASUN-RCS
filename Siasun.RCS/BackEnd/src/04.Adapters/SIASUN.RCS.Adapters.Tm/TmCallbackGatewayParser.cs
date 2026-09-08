using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Tasks;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Adapters.Tm
{
    /// <summary>
    /// TM 通配回调网关报文解析器
    /// 遵循《AGENTS.md》铁律 3：统一通过 TaskSerialRegistry 反查任务，严禁字符串截断或正则 hack
    /// </summary>
    public class TmCallbackGatewayParser : ITransientDependency
    {
        private readonly ITaskSerialRegistry _serialRegistry;
        private readonly ILogger<TmCallbackGatewayParser> _logger;

        public TmCallbackGatewayParser(
            ITaskSerialRegistry serialRegistry,
            ILogger<TmCallbackGatewayParser> logger)
        {
            _serialRegistry = serialRegistry;
            _logger = logger;
        }

        /// <summary>
        /// 解析 TM 异步上报的回调报文，并反查内部关联任务
        /// </summary>
        /// <param name="actionType">回调动作路由（例如 "action_done", "leg_completed", "agv_alarm"）</param>
        /// <param name="rawJson">原始 JSON 报文字符串</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>反解析出的统一回调事件模型</returns>
        public async Task<TmCallbackParsedResult> ParseAndResolveAsync(
            string actionType,
            string rawJson,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return new TmCallbackParsedResult(false, null, null, null, "回调载荷为空");
            }

            try
            {
                using var doc = JsonDocument.Parse(rawJson);
                var root = doc.RootElement;

                // 提取报文中的流水序列号（兼容多种工控报文格式）
                string? tmSerial = null;
                if (root.TryGetProperty("tmSerial", out var s1)) tmSerial = s1.GetString();
                else if (root.TryGetProperty("sequenceNo", out var s2)) tmSerial = s2.GetString();
                else if (root.TryGetProperty("serialNo", out var s3)) tmSerial = s3.GetString();
                else if (root.TryGetProperty("orderId", out var s4)) tmSerial = s4.GetString();

                // 提取车号
                string? agvCode = null;
                if (root.TryGetProperty("agvCode", out var a1)) agvCode = a1.GetString();
                else if (root.TryGetProperty("vehicleId", out var a2)) agvCode = a2.GetString();

                // 提取执行状态
                var isSuccess = true;
                if (root.TryGetProperty("success", out var succ)) isSuccess = succ.GetBoolean();
                else if (root.TryGetProperty("code", out var codeVal)) isSuccess = codeVal.GetInt32() == 0;

                string? errorMsg = null;
                if (root.TryGetProperty("message", out var msgElem)) errorMsg = msgElem.GetString();

                if (string.IsNullOrWhiteSpace(tmSerial))
                {
                    _logger.LogWarning("TM 回调报文中未找到有效的序列号字段: {Json}", rawJson);
                    return new TmCallbackParsedResult(false, null, agvCode, null, "报文中缺失 TM 序列号");
                }

                // 统一通过 TaskSerialRegistry 安全反查内部任务
                var mapping = await _serialRegistry.FindByTmSerialAsync(tmSerial, cancellationToken);
                if (mapping == null)
                {
                    _logger.LogWarning("TaskSerialRegistry 无法找到 TM 流水号 [{TmSerial}] 的内部任务映射", tmSerial);
                    return new TmCallbackParsedResult(false, tmSerial, agvCode, null, $"未找到序列号 [{tmSerial}] 的任务映射");
                }

                _logger.LogInformation("TM 回调成功匹配内部任务: TmSerial={TmSerial} -> TaskCode={TaskCode}, Leg={Leg}",
                    tmSerial, mapping.TaskCode, mapping.Leg);

                return new TmCallbackParsedResult(
                    Success: isSuccess,
                    TmSerial: tmSerial,
                    AgvCode: agvCode ?? mapping.VehicleCode,
                    TaskCode: mapping.TaskCode,
                    ErrorMessage: errorMsg,
                    Leg: mapping.Leg,
                    StepIndex: mapping.StepIndex,
                    WaitingEvent: mapping.WaitingEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析 TM 回调报文异常: Action={Action}, Json={Json}", actionType, rawJson);
                return new TmCallbackParsedResult(false, null, null, null, $"JSON 解析失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// TM 回调解析结果模型
    /// </summary>
    public record TmCallbackParsedResult(
        bool Success,
        string? TmSerial,
        string? AgvCode,
        string? TaskCode,
        string? ErrorMessage = null,
        string? Leg = null,
        int? StepIndex = null,
        string? WaitingEvent = null);
}
