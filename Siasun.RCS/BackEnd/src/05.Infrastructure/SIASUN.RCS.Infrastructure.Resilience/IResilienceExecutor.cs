using System;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Infrastructure.Resilience
{
    /// <summary>
    /// 工业级弹性策略通用执行器契约
    /// 基于 Polly v8 弹性策略（超时、指数抖动重试、断路器熔断），为 PLC、TM 与 MES/WMS 等外部交互提供高可靠容错底座
    /// </summary>
    public interface IResilienceExecutor : ISingletonDependency
    {
        /// <summary>
        /// 使用指定弹性策略管道执行带返回值的异步委托
        /// </summary>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <param name="action">异步工作委托</param>
        /// <param name="pipelineName">策略管道名称（如 "default", "plc_fast", "wms_long"）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>执行结果</returns>
        ValueTask<TResult> ExecuteAsync<TResult>(
            Func<CancellationToken, ValueTask<TResult>> action,
            string pipelineName = "default",
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 使用指定弹性策略管道执行无返回值的异步委托
        /// </summary>
        /// <param name="action">异步工作委托</param>
        /// <param name="pipelineName">策略管道名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>执行任务</returns>
        ValueTask ExecuteAsync(
            Func<CancellationToken, ValueTask> action,
            string pipelineName = "default",
            CancellationToken cancellationToken = default);
    }
}
