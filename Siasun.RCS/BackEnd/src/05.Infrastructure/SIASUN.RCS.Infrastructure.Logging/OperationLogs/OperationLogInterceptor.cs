using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Interfaces.OperationLogs;
using SIASUN.RCS.Logs.OperatorLog;
using SIASUN.RCS.Logs.OperatorLogs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DynamicProxy;

namespace SIASUN.RCS.Infrastructure.Logging.OperationLogs
{
    /// <summary>
    /// 声明式业务操作审计拦截器
    /// 拦截标记有 <see cref="OperationLogAttribute"/> 的应用服务方法，零侵入自动捕获操作人、入参、执行结果与异常
    /// </summary>
    public class OperationLogInterceptor : AbpInterceptor, ITransientDependency
    {
        private readonly IOperationLogRecorder _recorder;
        private readonly ILogger<OperationLogInterceptor> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// 构造函数注入操作日志记录器与系统日志
        /// </summary>
        /// <param name="recorder">操作审计记录器</param>
        /// <param name="logger">日志服务</param>
        public OperationLogInterceptor(
            IOperationLogRecorder recorder,
            ILogger<OperationLogInterceptor> logger)
        {
            _recorder = recorder;
            _logger = logger;
        }

        /// <summary>
        /// 异步拦截目标方法调用并执行声明式操作审计
        /// </summary>
        /// <param name="invocation">方法调用上下文</param>
        public override async Task InterceptAsync(IAbpMethodInvocation invocation)
        {
            var targetObjType = invocation.TargetObject?.GetType();
            var attr = (invocation.Method != null ? invocation.Method.GetCustomAttribute<OperationLogAttribute>(true) : null)
                       ?? (targetObjType != null ? targetObjType.GetCustomAttribute<OperationLogAttribute>(true) : null);

            if (attr == null)
            {
                await invocation.ProceedAsync();
                return;
            }

            var module = !string.IsNullOrWhiteSpace(attr.Module)
                ? attr.Module
                : NormalizeModuleName(targetObjType?.Name ?? "System");

            var action = !string.IsNullOrWhiteSpace(attr.Action)
                ? attr.Action
                : NormalizeActionName(invocation.Method?.Name ?? "Execute");

            var targetType = attr.TargetType ?? "System";
            var targetId = ResolveTargetId(invocation.ArgumentsDictionary);

            string? argsJson = null;
            if (attr.CaptureArguments && invocation.ArgumentsDictionary != null && invocation.ArgumentsDictionary.Count > 0)
            {
                try
                {
                    var sanitized = invocation.ArgumentsDictionary
                        .Where(kvp => !IsSensitiveKey(kvp.Key))
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    argsJson = JsonSerializer.Serialize(sanitized, JsonOptions);
                }
                catch (Exception ex)
                {
                    argsJson = $"[SerializeFailed: {ex.Message}]";
                }
            }

            var desc = attr.Description ?? $"执行操作 {module}.{action}";
            if (!string.IsNullOrEmpty(argsJson))
            {
                desc += $" 入参: {argsJson}";
            }

            var context = new OperationLogContext
            {
                Module = module,
                Action = action,
                TargetType = targetType,
                TargetId = targetId ?? string.Empty,
                Description = desc
            };

            var prevRecorded = OperationLogScope.IsRecorded;
            OperationLogScope.IsRecorded = false;
            try
            {
                await invocation.ProceedAsync();

                if (!OperationLogScope.IsRecorded)
                {
                    context.AfterState = "Success";
                    _recorder.Record(context, OperationLogStatus.Success);
                }
            }
            catch (Exception ex)
            {
                if (!OperationLogScope.IsRecorded)
                {
                    context.AfterState = "Failed";
                    _recorder.Record(context, OperationLogStatus.Failed, ex.Message);
                }
                throw;
            }
            finally
            {
                OperationLogScope.IsRecorded = prevRecorded;
            }
        }

        /// <summary>
        /// 操作日志作用域标记，用于识别当前异步调用链中是否已经显式记录过操作审计，防止 AOP 与显式记录重复落库
        /// </summary>
        internal static class OperationLogScope
        {
            private static readonly System.Threading.AsyncLocal<bool> _isRecorded = new();

            /// <summary>
            /// 当前异步作用域中是否已完成操作审计记录
            /// </summary>
            public static bool IsRecorded
            {
                get => _isRecorded.Value;
                set => _isRecorded.Value = value;
            }
        }

        private static string NormalizeModuleName(string typeName)
        {
            if (typeName.EndsWith("AppService", StringComparison.OrdinalIgnoreCase))
                return typeName[..^"AppService".Length];
            if (typeName.EndsWith("Service", StringComparison.OrdinalIgnoreCase))
                return typeName[..^"Service".Length];
            return typeName;
        }

        private static string NormalizeActionName(string methodName)
        {
            if (methodName.EndsWith("Async", StringComparison.OrdinalIgnoreCase))
                return methodName[..^"Async".Length];
            return methodName;
        }

        private static string? ResolveTargetId(IReadOnlyDictionary<string, object?>? arguments)
        {
            if (arguments == null || arguments.Count == 0) return null;

            foreach (var kvp in arguments)
            {
                if (kvp.Value == null) continue;

                var key = kvp.Key;
                if (key.Equals("id", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("taskId", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("vehicleId", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("code", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("key", StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value.ToString();
                }

                // 若入参为复杂对象，尝试反射读取 Id/TaskId/Code 属性
                var prop = kvp.Value.GetType().GetProperty("Id")
                           ?? kvp.Value.GetType().GetProperty("TaskId")
                           ?? kvp.Value.GetType().GetProperty("VehicleId")
                           ?? kvp.Value.GetType().GetProperty("Code");
                if (prop != null)
                {
                    var val = prop.GetValue(kvp.Value);
                    if (val != null) return val.ToString();
                }
            }

            return null;
        }

        private static bool IsSensitiveKey(string key)
        {
            return key.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                   key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                   key.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
                   key.Contains("credential", StringComparison.OrdinalIgnoreCase) ||
                   key.Contains("pwd", StringComparison.OrdinalIgnoreCase);
        }
    }
}
