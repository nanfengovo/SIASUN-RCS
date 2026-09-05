using System;
using SIASUN.RCS.Interfaces.OperationLogs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Tracing;
using Volo.Abp.Users;
using SIASUN.RCS.Logs.OperatorLogs;
using SIASUN.RCS.Logs.OperatorLog;

namespace SIASUN.RCS.Infrastructure.Logging.OperationLogs
{
    public class OperationLogRecorder : IOperationLogRecorder, ITransientDependency
    {

        private readonly OperationLogChannelManager _channelManager;

        private readonly ICurrentUser _currentUser;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly Diagnostics.SignalR.IDiagnosticLiveStreamBroker? _liveStreamBroker;

        public OperationLogRecorder(
            OperationLogChannelManager channelManager,
            ICurrentUser currentUser,
            ICorrelationIdProvider correlationIdProvider,
            Diagnostics.SignalR.IDiagnosticLiveStreamBroker? liveStreamBroker = null)
        {
            _channelManager = channelManager;
            _currentUser = currentUser;
            _correlationIdProvider = correlationIdProvider;
            _liveStreamBroker = liveStreamBroker;
        }

        public void RecordFailure(string module, string action, string targetType, string targetKey, string description, string errorMessage)
        {
            var log = CreateLogObj(module, action, targetType, targetKey, description, OperationLogStatus.Failed, errorMessage);
            _channelManager.Channel.Writer.TryWrite(log);

            if (_liveStreamBroker != null && _liveStreamBroker.IsEnabled)
            {
                _liveStreamBroker.Publish(new Diagnostics.SignalR.LiveEventDto
                {
                    Timestamp = DateTime.UtcNow,
                    Track = "Operator",
                    Level = "Error",
                    Source = _currentUser.UserName ?? "System",
                    Title = $"[{module}] {action} 失败 ({targetType}:{targetKey})",
                    Summary = $"{description} - 错误: {errorMessage}",
                    TraceId = _correlationIdProvider.Get(),
                    TargetId = string.Equals(targetType, "Task", StringComparison.OrdinalIgnoreCase) ? targetKey : null,
                    VehicleId = string.Equals(targetType, "Vehicle", StringComparison.OrdinalIgnoreCase) ? targetKey : null
                });
            }
        }

        public void RecordSuccess(string module, string action, string targetType, string targetKey, string description)
        {
            var log = CreateLogObj(module, action, targetType, targetKey, description, OperationLogStatus.Success, null);
            _channelManager.Channel.Writer.TryWrite(log);

            if (_liveStreamBroker != null && _liveStreamBroker.IsEnabled)
            {
                _liveStreamBroker.Publish(new Diagnostics.SignalR.LiveEventDto
                {
                    Timestamp = DateTime.UtcNow,
                    Track = "Operator",
                    Level = "Information",
                    Source = _currentUser.UserName ?? "System",
                    Title = $"[{module}] {action} 成功 ({targetType}:{targetKey})",
                    Summary = description,
                    TraceId = _correlationIdProvider.Get(),
                    TargetId = string.Equals(targetType, "Task", StringComparison.OrdinalIgnoreCase) ? targetKey : null,
                    VehicleId = string.Equals(targetType, "Vehicle", StringComparison.OrdinalIgnoreCase) ? targetKey : null
                });
            }
        }

        private OperationLog CreateLogObj(string module, string action, string targetType, string targetKey, string description, OperationLogStatus status, string errorMessage)
        {
            var userId = _currentUser.Id;
            var userName = _currentUser.UserName ?? "System";
            var operatorType = _currentUser.Id.HasValue ? OperatorType.User : OperatorType.System;
            var correlationId = _correlationIdProvider.Get();

            // ABP 实际上没有自带 IClientInfoProvider (通常在 Web 层有，这里可以通过其他方式获取 IP，由于是在 Infrastructure 层，为了保持简单，先用占位或者空值)
            // 如果需要真实 IP 可以注入 IHttpContextAccessor，但为了避免直接依赖 Web 组件，用空或者通过依赖抽象传入。
            var clientIp = string.Empty;

            return new OperationLog(
                id: Guid.NewGuid(),
                operatorType: operatorType,
                userId: userId,
                userName: userName,
                clientIp: clientIp,
                correlationId: correlationId,
                module: module,
                action: action,
                targetType: targetType,
                targetId: targetKey,
                status: status,
                description: description,
                errorMessage: errorMessage
            );
        }
    }
}