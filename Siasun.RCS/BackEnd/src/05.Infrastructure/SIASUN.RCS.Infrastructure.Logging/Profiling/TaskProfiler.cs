using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Diagnostics;
using SIASUN.RCS.Profiling;
using SIASUN.RCS.Tasks.Profiling;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Infrastructure.Logging.Profiling
{
    /// <summary>
    /// 任务步骤剖析器实现
    /// 基于内存无锁 Channel 实现零开销的步骤耗时捕获与调度链路解耦
    /// </summary>
    public class TaskProfiler : ITaskProfiler, ISingletonDependency
    {
        private readonly TaskProfilingChannel _channel;
        private readonly ILogger<TaskProfiler> _logger;

        public TaskProfiler(TaskProfilingChannel channel, ILogger<TaskProfiler> logger)
        {
            _channel = channel;
            _logger = logger;
        }

        /// <inheritdoc />
        public IDisposable BeginStep(
            string taskCode,
            string subsystem,
            string operationName,
            string? agvId = null,
            int stepIndex = 0,
            string? activeLeg = null,
            string? summary = null,
            string? batchId = null)
        {
            return new ProfilingScope(this, taskCode, subsystem, operationName, agvId, stepIndex, activeLeg, summary, batchId);
        }

        /// <inheritdoc />
        public void RecordStep(TaskStepProfilingRecord record)
        {
            if (record == null) return;

            var entity = new TaskStepProfiling(
                id: Guid.NewGuid(),
                taskCode: record.TaskCode,
                subsystem: record.Subsystem,
                operationName: record.OperationName,
                startTime: record.StartTime,
                endTime: record.EndTime,
                durationMs: record.DurationMs,
                status: record.Status,
                summary: record.Summary ?? string.Empty,
                batchId: record.BatchId,
                agvId: record.AgvId,
                stepIndex: record.StepIndex,
                activeLeg: record.ActiveLeg,
                details: record.Details,
                traceId: record.TraceId ?? RcsTraceContext.CurrentTraceId
            );

            if (!_channel.Writer.TryWrite(entity))
            {
                _logger.LogWarning("TaskProfilingChannel 达到背压上限，丢弃旧条目以保全主调度性能: TaskCode={TaskCode}, Op={Op}", record.TaskCode, record.OperationName);
            }
        }

        /// <inheritdoc />
        public void RecordStep(
            string taskCode,
            string subsystem,
            string operationName,
            long durationMs,
            string status = "Success",
            string? summary = null,
            string? agvId = null,
            int stepIndex = 0,
            string? activeLeg = null,
            string? batchId = null,
            string? details = null)
        {
            var now = DateTime.UtcNow;
            var start = now.AddMilliseconds(-Math.Max(0, durationMs));

            RecordStep(new TaskStepProfilingRecord
            {
                TaskCode = taskCode,
                Subsystem = subsystem,
                OperationName = operationName,
                StartTime = start,
                EndTime = now,
                DurationMs = durationMs,
                Status = status,
                Summary = summary ?? $"{operationName} ({durationMs}ms)",
                AgvId = agvId,
                StepIndex = stepIndex,
                ActiveLeg = activeLeg,
                BatchId = batchId,
                Details = details,
                TraceId = RcsTraceContext.CurrentTraceId
            });
        }

        private sealed class ProfilingScope : IDisposable
        {
            private readonly TaskProfiler _profiler;
            private readonly string _taskCode;
            private readonly string _subsystem;
            private readonly string _operationName;
            private readonly string? _agvId;
            private readonly int _stepIndex;
            private readonly string? _activeLeg;
            private readonly string? _summary;
            private readonly string? _batchId;
            private readonly Stopwatch _stopwatch;
            private readonly DateTime _startTime;
            private bool _disposed;

            public ProfilingScope(
                TaskProfiler profiler,
                string taskCode,
                string subsystem,
                string operationName,
                string? agvId,
                int stepIndex,
                string? activeLeg,
                string? summary,
                string? batchId)
            {
                _profiler = profiler;
                _taskCode = taskCode;
                _subsystem = subsystem;
                _operationName = operationName;
                _agvId = agvId;
                _stepIndex = stepIndex;
                _activeLeg = activeLeg;
                _summary = summary;
                _batchId = batchId;
                _startTime = DateTime.UtcNow;
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                _stopwatch.Stop();
                var endTime = DateTime.UtcNow;
                var elapsedMs = _stopwatch.ElapsedMilliseconds;

                _profiler.RecordStep(new TaskStepProfilingRecord
                {
                    TaskCode = _taskCode,
                    Subsystem = _subsystem,
                    OperationName = _operationName,
                    StartTime = _startTime,
                    EndTime = endTime,
                    DurationMs = elapsedMs,
                    Status = "Success",
                    Summary = _summary ?? $"{_operationName} ({elapsedMs}ms)",
                    AgvId = _agvId,
                    StepIndex = _stepIndex,
                    ActiveLeg = _activeLeg,
                    BatchId = _batchId,
                    TraceId = RcsTraceContext.CurrentTraceId
                });
            }
        }
    }
}
