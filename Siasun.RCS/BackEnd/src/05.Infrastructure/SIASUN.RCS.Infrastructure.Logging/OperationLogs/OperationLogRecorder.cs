using System;
using Microsoft.AspNetCore.Http;
using SIASUN.RCS.Interfaces.OperationLogs;
using SIASUN.RCS.Logs.OperatorLog;
using SIASUN.RCS.Logs.OperatorLogs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Tracing;
using Volo.Abp.Users;

namespace SIASUN.RCS.Infrastructure.Logging.OperationLogs
{
    /// <summary>
    /// 调度员操作与系统自审计日志记录器实现
    /// </summary>
    [Dependency(ReplaceServices = true)]
    public class OperationLogRecorder : IOperationLogRecorder, ITransientDependency
    {
        private readonly OperationLogChannelManager _channelManager;
        private readonly ICurrentUser _currentUser;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly Diagnostics.SignalR.IDiagnosticLiveStreamBroker? _liveStreamBroker;
        private readonly IHttpContextAccessor? _httpContextAccessor;

        /// <summary>
        /// 构造函数注入所需依赖
        /// </summary>
        public OperationLogRecorder(
            OperationLogChannelManager channelManager,
            ICurrentUser currentUser,
            ICorrelationIdProvider correlationIdProvider,
            Diagnostics.SignalR.IDiagnosticLiveStreamBroker? liveStreamBroker = null,
            IHttpContextAccessor? httpContextAccessor = null)
        {
            _channelManager = channelManager;
            _currentUser = currentUser;
            _correlationIdProvider = correlationIdProvider;
            _liveStreamBroker = liveStreamBroker;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 记录包含完整上下文的操作审计条目（推荐使用）
        /// </summary>
        /// <param name="context">操作审计上下文</param>
        /// <param name="status">执行状态</param>
        /// <param name="errorMessage">失败时的错误消息</param>
        public void Record(OperationLogContext context, OperationLogStatus status = OperationLogStatus.Success, string? errorMessage = null)
        {
            if (context == null) return;
            OperationLogInterceptor.OperationLogScope.IsRecorded = true;

            var userId = _currentUser.Id;
            var userName = _currentUser.UserName ?? "System";
            var operatorType = _currentUser.Id.HasValue ? OperatorType.User : OperatorType.System;
            var httpContext = _httpContextAccessor?.HttpContext;
            var correlationId = !string.IsNullOrWhiteSpace(context.CorrelationId)
                ? context.CorrelationId
                : (SIASUN.RCS.Diagnostics.RcsTraceContext.CurrentTraceId
                   ?? httpContext?.Items["__RcsCorrelationId"]?.ToString()
                   ?? (httpContext?.Request.Headers.TryGetValue("X-Correlation-Id", out var cid) == true ? cid.ToString() : null)
                   ?? (httpContext?.Request.Headers.TryGetValue("X-Trace-Id", out var tid) == true ? tid.ToString() : null)
                   ?? _correlationIdProvider.Get()
                   ?? string.Empty);

            var clientIp = !string.IsNullOrWhiteSpace(context.ClientIp)
                ? context.ClientIp
                : (_httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? string.Empty);

            var taskId = !string.IsNullOrWhiteSpace(context.TaskId)
                ? context.TaskId
                : (string.Equals(context.TargetType, "Task", StringComparison.OrdinalIgnoreCase) ? context.TargetId : null);

            var agvId = !string.IsNullOrWhiteSpace(context.AgvId)
                ? context.AgvId
                : (string.Equals(context.TargetType, "Vehicle", StringComparison.OrdinalIgnoreCase) ? context.TargetId : null);

            var log = new OperationLog(
                id: Guid.NewGuid(),
                operatorType: operatorType,
                userId: userId,
                userName: userName,
                clientIp: clientIp,
                correlationId: correlationId,
                module: context.Module,
                action: context.Action,
                targetType: context.TargetType,
                targetId: context.TargetId,
                status: status,
                description: context.Description,
                errorMessage: errorMessage,
                creationTime: DateTime.UtcNow,
                beforeState: context.BeforeState,
                afterState: context.AfterState,
                reason: context.Reason,
                taskId: taskId,
                agvId: agvId,
                elapsedMilliseconds: context.ElapsedMilliseconds
            );

            if (!_channelManager.Channel.Writer.TryWrite(log))
            {
                // 铁证特权通道在突发排队时等待至多 2 秒，确保调度员操作与自愈记录不可抵赖
                var written = false;
                try
                {
                    var writeTask = _channelManager.Channel.Writer.WriteAsync(log).AsTask();
                    written = writeTask.Wait(TimeSpan.FromSeconds(2));
                    if (!written)
                    {
                        Console.Error.WriteLine($"[EMERGENCY-AUDIT-LOSS-PREVENTION] OperationLog channel full, timed out writing: {log.Action} ({log.TargetType}:{log.TargetId})");
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[EMERGENCY-AUDIT-LOSS-PREVENTION] OperationLog write failed: {ex.Message}");
                }

                // 若 2 秒等待超时或写入出现异常，坚决推入紧急溢出保全环并应急落盘，达成 L4 工业级零丢失
                if (!written)
                {
                    _channelManager.SpillBuffer.Enqueue(log);
                }
            }

            if (_liveStreamBroker != null && _liveStreamBroker.IsEnabled)
            {
                var isFailed = status == OperationLogStatus.Failed;
                var stateInfo = !string.IsNullOrEmpty(context.BeforeState) || !string.IsNullOrEmpty(context.AfterState)
                    ? $" [{context.BeforeState} -> {context.AfterState}]"
                    : string.Empty;

                _liveStreamBroker.Publish(new Diagnostics.SignalR.LiveEventDto
                {
                    Timestamp = DateTime.UtcNow,
                    Track = SIASUN.RCS.Diagnostics.DiagnosticTracks.Operator,
                    Level = isFailed ? SIASUN.RCS.Diagnostics.DiagnosticLevels.Error : SIASUN.RCS.Diagnostics.DiagnosticLevels.Information,
                    Source = userName,
                    Title = $"[{context.Module}] {context.Action} {(isFailed ? "失败" : "成功")} ({context.TargetType}:{context.TargetId}){stateInfo}",
                    Summary = isFailed
                        ? $"{context.Description} - 错误: {errorMessage}"
                        : (string.IsNullOrWhiteSpace(context.Reason) ? context.Description : $"{context.Description} (原因: {context.Reason})"),
                    TraceId = correlationId,
                    TargetId = taskId,
                    VehicleId = agvId
                });
            }
        }

        /// <summary>
        /// 记录失败操作
        /// </summary>
        public void RecordFailure(string module, string action, string targetType, string targetKey, string description, string errorMessage)
        {
            Record(new OperationLogContext
            {
                Module = module,
                Action = action,
                TargetType = targetType,
                TargetId = targetKey,
                Description = description
            }, OperationLogStatus.Failed, errorMessage);
        }

        /// <summary>
        /// 记录成功操作
        /// </summary>
        public void RecordSuccess(string module, string action, string targetType, string targetKey, string description)
        {
            Record(new OperationLogContext
            {
                Module = module,
                Action = action,
                TargetType = targetType,
                TargetId = targetKey,
                Description = description
            }, OperationLogStatus.Success, null);
        }
    }
}