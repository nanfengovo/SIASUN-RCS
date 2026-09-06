using System;
using System.Threading;

namespace SIASUN.RCS.Diagnostics
{
    /// <summary>
    /// 全局统一追踪上下文（Ambient AsyncLocal 锚点）
    /// 确保在同一异步执行链路（无论 HTTP 请求、后台任务或跨线程异步等待）中
    /// TraceId / CorrelationId 绝对 100% 字节级一致，杜绝任何分叉隐患
    /// </summary>
    public static class RcsTraceContext
    {
        private static readonly AsyncLocal<string?> _currentTraceId = new();

        /// <summary>
        /// 获取或设置当前异步链路的全局唯一 TraceId
        /// </summary>
        public static string? CurrentTraceId
        {
            get => _currentTraceId.Value;
            set => _currentTraceId.Value = value;
        }

        /// <summary>
        /// 开启一个受保护的作用域并在退出时自动恢复前一个 TraceId
        /// </summary>
        /// <param name="traceId">要在当前作用域生效的 TraceId</param>
        /// <returns>IDisposable 作用域控制对象</returns>
        public static IDisposable SetScoped(string traceId)
        {
            var previous = _currentTraceId.Value;
            _currentTraceId.Value = traceId;
            return new DisposeAction(() => _currentTraceId.Value = previous);
        }

        private sealed class DisposeAction : IDisposable
        {
            private readonly Action _action;
            public DisposeAction(Action action) => _action = action;
            public void Dispose() => _action();
        }
    }
}
