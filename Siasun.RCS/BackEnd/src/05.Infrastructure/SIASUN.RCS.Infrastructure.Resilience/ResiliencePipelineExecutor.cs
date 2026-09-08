using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace SIASUN.RCS.Infrastructure.Resilience
{
    /// <summary>
    /// 基于 Polly v8 的工业级通信弹性策略通用执行器实现
    /// 提供毫秒级超时控制、指数抖动退避重试（避免羊群效应）与熔断降级保护
    /// </summary>
    public class ResiliencePipelineExecutor : IResilienceExecutor
    {
        private readonly ILogger<ResiliencePipelineExecutor> _logger;
        private readonly ConcurrentDictionary<string, ResiliencePipeline> _pipelines = new(StringComparer.OrdinalIgnoreCase);

        public ResiliencePipelineExecutor(ILogger<ResiliencePipelineExecutor> logger)
        {
            _logger = logger;
            _pipelines["default"] = CreateDefaultPipeline(logger);
            _pipelines["plc_fast"] = CreatePlcFastPipeline(logger);
        }

        /// <inheritdoc />
        public ValueTask<TResult> ExecuteAsync<TResult>(
            Func<CancellationToken, ValueTask<TResult>> action,
            string pipelineName = "default",
            CancellationToken cancellationToken = default)
        {
            var pipeline = _pipelines.GetOrAdd(pipelineName, name => CreateDefaultPipeline(_logger));
            return pipeline.ExecuteAsync(action, cancellationToken);
        }

        /// <inheritdoc />
        public ValueTask ExecuteAsync(
            Func<CancellationToken, ValueTask> action,
            string pipelineName = "default",
            CancellationToken cancellationToken = default)
        {
            var pipeline = _pipelines.GetOrAdd(pipelineName, name => CreateDefaultPipeline(_logger));
            return pipeline.ExecuteAsync(action, cancellationToken);
        }

        private static ResiliencePipeline CreateDefaultPipeline(ILogger logger)
        {
            return new ResiliencePipelineBuilder()
                .AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(10),
                    OnTimeout = args =>
                    {
                        logger.LogWarning("弹性策略超时触发: Timeout={Timeout}s", args.Timeout.TotalSeconds);
                        return default;
                    }
                })
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    Delay = TimeSpan.FromMilliseconds(200),
                    OnRetry = args =>
                    {
                        logger.LogWarning("弹性重试中 (第 {Attempt} 次): 延迟={Delay}ms, 原因={Reason}",
                            args.AttemptNumber + 1, args.RetryDelay.TotalMilliseconds, args.Outcome.Exception?.Message);
                        return default;
                    }
                })
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(15),
                    MinimumThroughput = 5,
                    BreakDuration = TimeSpan.FromSeconds(10),
                    OnOpened = args =>
                    {
                        logger.LogError("通信熔断器触发打开，系统进入降级保护模式: 熔断时长={Duration}s", args.BreakDuration.TotalSeconds);
                        return default;
                    },
                    OnClosed = _ =>
                    {
                        logger.LogInformation("通信熔断器已闭合复位，恢复正常网络投递");
                        return default;
                    }
                })
                .Build();
        }

        private static ResiliencePipeline CreatePlcFastPipeline(ILogger logger)
        {
            return new ResiliencePipelineBuilder()
                .AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(3)
                })
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 2,
                    BackoffType = DelayBackoffType.Constant,
                    Delay = TimeSpan.FromMilliseconds(100)
                })
                .Build();
        }
    }
}
