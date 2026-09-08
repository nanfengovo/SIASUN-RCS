using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Interfaces.OperationLogs;
using SIASUN.RCS.Logs.OperatorLog;
using SIASUN.RCS.Logs.OperatorLogs;
using SIASUN.RCS.Profiling;
using SIASUN.RCS.Tasks.Profiling;

namespace SIASUN.RCS.Commands
{
    /// <summary>
    /// CQRS 全局命令自动审计管道行为
    /// 统一拦截所有实现了 ICommand 或 IAuditableCommand 的业务状态变更命令，
    /// 全自动抓取命令参数、执行耗时、变迁前后状态快照并落盘 OperationLog，
    /// 彻底消除业务 CommandHandler 内部的手动日志拼接与 try-catch 样板代码。
    /// </summary>
    /// <typeparam name="TRequest">请求对象类型</typeparam>
    /// <typeparam name="TResponse">响应对象类型</typeparam>
    public class CommandAuditPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IOperationLogRecorder _operationLogRecorder;
        private readonly ILogger<CommandAuditPipelineBehavior<TRequest, TResponse>> _logger;
        private readonly ITaskProfiler? _taskProfiler;

        /// <summary>
        /// 构造函数注入操作审计记录器与日志器
        /// </summary>
        /// <param name="operationLogRecorder">操作审计记录器</param>
        /// <param name="logger">日志器</param>
        /// <param name="taskProfiler">任务步骤剖析器（可选）</param>
        public CommandAuditPipelineBehavior(
            IOperationLogRecorder operationLogRecorder,
            ILogger<CommandAuditPipelineBehavior<TRequest, TResponse>> logger,
            ITaskProfiler? taskProfiler = null)
        {
            _operationLogRecorder = operationLogRecorder;
            _logger = logger;
            _taskProfiler = taskProfiler;
        }

        /// <summary>
        /// 管道拦截处理
        /// </summary>
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // 若不是领域变更命令且未标记审计，则直接穿透，不进行审计（避免对只读 Query 造成额外开销）
            var isAuditable = request is IAuditableCommand || IsCommandType(typeof(TRequest));
            if (!isAuditable)
            {
                return await next(cancellationToken);
            }

            var (module, action, targetType, targetId, taskId, agvId, reason, description) = ExtractMetadata(request);
            var sw = Stopwatch.StartNew();

            try
            {
                var response = await next(cancellationToken);
                sw.Stop();

                string? beforeState = null;
                string? afterState = null;

                if (response is IStateTransitionResult transition)
                {
                    beforeState = transition.BeforeState;
                    afterState = transition.AfterState;
                    targetType = !string.IsNullOrWhiteSpace(transition.TargetType) ? transition.TargetType : targetType;
                    targetId = !string.IsNullOrWhiteSpace(transition.TargetId) ? transition.TargetId : targetId;
                }

                _operationLogRecorder.Record(new OperationLogContext
                {
                    Module = module,
                    Action = action,
                    TargetType = targetType,
                    TargetId = targetId ?? string.Empty,
                    TaskId = taskId,
                    AgvId = agvId,
                    BeforeState = beforeState,
                    AfterState = afterState,
                    Reason = reason,
                    Description = $"{description} (耗时: {sw.ElapsedMilliseconds}ms)",
                    ElapsedMilliseconds = sw.ElapsedMilliseconds
                }, OperationLogStatus.Success);

                if (!string.IsNullOrWhiteSpace(taskId))
                {
                    _taskProfiler?.RecordStep(
                        taskCode: taskId,
                        subsystem: ProfilingSubsystem.Dispatcher,
                        operationName: action,
                        durationMs: sw.ElapsedMilliseconds,
                        status: "Success",
                        summary: description,
                        agvId: agvId);
                }

                return response;
            }
            catch (Exception ex)
            {
                sw.Stop();

                _operationLogRecorder.Record(new OperationLogContext
                {
                    Module = module,
                    Action = action,
                    TargetType = targetType,
                    TargetId = targetId ?? string.Empty,
                    TaskId = taskId,
                    AgvId = agvId,
                    BeforeState = null,
                    AfterState = null,
                    Reason = reason,
                    Description = $"{description} 失败：{ex.Message} (耗时: {sw.ElapsedMilliseconds}ms)",
                    ElapsedMilliseconds = sw.ElapsedMilliseconds
                }, OperationLogStatus.Failed, ex.Message);

                if (!string.IsNullOrWhiteSpace(taskId))
                {
                    _taskProfiler?.RecordStep(
                        taskCode: taskId,
                        subsystem: ProfilingSubsystem.Dispatcher,
                        operationName: action,
                        durationMs: sw.ElapsedMilliseconds,
                        status: "Failed",
                        summary: $"{description} 失败：{ex.Message}",
                        agvId: agvId,
                        details: ex.Message);
                }

                throw;
            }
        }

        private static bool IsCommandType(Type type)
        {
            return typeof(ICommand).IsAssignableFrom(type) ||
                   type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));
        }

        private static (string Module, string Action, string TargetType, string? TargetId, string? TaskId, string? AgvId, string? Reason, string Description) ExtractMetadata(TRequest request)
        {
            if (request is IAuditableCommand auditable)
            {
                return (
                    auditable.Module,
                    auditable.Action,
                    auditable.TargetType,
                    auditable.TargetId,
                    auditable.TaskId,
                    auditable.AgvId,
                    auditable.Reason,
                    auditable.Description ?? $"执行命令 [{auditable.Action}]"
                );
            }

            var typeName = typeof(TRequest).Name;
            var actionName = typeName.EndsWith("Command", StringComparison.Ordinal) ? typeName[..^7] : typeName;
            return (
                "Application",
                actionName,
                "Command",
                null,
                null,
                null,
                null,
                $"执行命令 [{actionName}]"
            );
        }
    }
}

