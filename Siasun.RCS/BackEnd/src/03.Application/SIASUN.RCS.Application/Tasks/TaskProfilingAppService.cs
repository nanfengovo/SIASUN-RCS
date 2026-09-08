using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SIASUN.RCS.Logs.OperatorLog;
using SIASUN.RCS.Logs.OperatorLogs;
using SIASUN.RCS.Profiling;
using SIASUN.RCS.Tasks.Dtos;
using SIASUN.RCS.Tasks.Profiling;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace SIASUN.RCS.Tasks
{
    /// <summary>
    /// 任务步骤剖析与时序流转看板应用服务实现
    /// </summary>
    public class TaskProfilingAppService : ApplicationService, ITaskProfilingAppService
    {
        private readonly IRepository<TaskStepProfiling, Guid> _profilingRepository;
        private readonly IRepository<AgvTask, Guid> _taskRepository;
        private readonly IRepository<OperationLog, Guid> _operationLogRepository;
        private readonly IAsyncQueryableExecuter? _customAsyncExecuter;

        /// <summary>
        /// 优先使用显式注入的 AsyncExecuter，其次回退至 ABP 基类 LazyServiceProvider 提供的实例
        /// </summary>
        protected new IAsyncQueryableExecuter AsyncExecuter => _customAsyncExecuter ?? base.AsyncExecuter;

        public TaskProfilingAppService(
            IRepository<TaskStepProfiling, Guid> profilingRepository,
            IRepository<AgvTask, Guid> taskRepository,
            IRepository<OperationLog, Guid> operationLogRepository,
            IAsyncQueryableExecuter? asyncExecuter = null)
        {
            _profilingRepository = profilingRepository;
            _taskRepository = taskRepository;
            _operationLogRepository = operationLogRepository;
            _customAsyncExecuter = asyncExecuter;
        }

        /// <inheritdoc />
        public async Task<TaskTimelineProfilingDto> GetTaskTimelineProfilingAsync(string taskCode)
        {
            if (string.IsNullOrWhiteSpace(taskCode))
            {
                return new TaskTimelineProfilingDto();
            }

            var taskQuery = await _taskRepository.GetQueryableAsync();
            var task = await AsyncExecuter.FirstOrDefaultAsync(taskQuery.Where(t => t.TaskCode == taskCode));

            var stepQuery = await _profilingRepository.GetQueryableAsync();
            var steps = await AsyncExecuter.ToListAsync(
                stepQuery.Where(s => s.TaskCode == taskCode)
                         .OrderBy(s => s.StartTime)
            );

            var result = new TaskTimelineProfilingDto
            {
                TaskCode = taskCode,
                State = task != null ? task.Status.ToString() : (steps.Count > 0 ? "Running" : "Pending"),
                TaskType = "Transport",
                FromStation = task?.FromStation,
                ToStation = task?.ToStation,
                CarrierCode = task?.CarrierCode,
                BatchId = task?.BatchId,
                AssignedVehicleCode = task?.AssignedVehicleCode,
                CreationTime = task?.CreationTime ?? (steps.FirstOrDefault()?.StartTime ?? DateTime.UtcNow),
                StartTime = task?.StartTime ?? steps.FirstOrDefault()?.StartTime,
                EndTime = task?.EndTime ?? (task?.Status == AgvTaskStatus.Succeeded || task?.Status == AgvTaskStatus.Failed || task?.Status == AgvTaskStatus.Canceled ? steps.LastOrDefault()?.EndTime : null)
            };

            if (result.StartTime.HasValue && result.EndTime.HasValue)
            {
                result.TotalDurationMs = Math.Max(0, (long)(result.EndTime.Value - result.StartTime.Value).TotalMilliseconds);
            }
            else if (steps.Count > 0)
            {
                result.TotalDurationMs = steps.Sum(s => s.DurationMs);
            }

            // 1. 组装 TM 底层状态流转节点
            result.TmStages = BuildTmStages(task, steps);

            // 2. 组装上游出库阶段与批次状态
            result.UpstreamBatches = BuildUpstreamBatches(task, steps);

            // 3. 组装系统交互流水项
            result.Interactions = steps.Select(s => new SubsystemInteractionItemDto
            {
                Timestamp = s.StartTime,
                Subsystem = s.Subsystem,
                Event = s.OperationName,
                Status = s.Status,
                ElapsedMs = s.DurationMs,
                Summary = !string.IsNullOrWhiteSpace(s.Summary) ? s.Summary : $"{s.OperationName} ({s.DurationMs}ms)",
                Details = s.Details
            }).ToList();

            // 4. 组装耗时拆解度量
            result.MetricsBreakdown = BuildMetricsBreakdown(result.TotalDurationMs, steps);

            return result;
        }

        /// <inheritdoc />
        public async Task<TaskExecutionMetricsSummaryDto> GetMetricsSummaryAsync(GetMetricsSummaryInput input)
        {
            var startTime = input.StartTime ?? DateTime.UtcNow.AddDays(-1);
            var endTime = input.EndTime ?? DateTime.UtcNow;

            var taskQuery = await _taskRepository.GetQueryableAsync();
            var tasks = await AsyncExecuter.ToListAsync(
                taskQuery.Where(t => (t.CreationTime >= startTime && t.CreationTime <= endTime) || (t.StartTime.HasValue && t.StartTime.Value >= startTime && t.StartTime.Value <= endTime))
            );

            var stepQuery = await _profilingRepository.GetQueryableAsync();
            var steps = await AsyncExecuter.ToListAsync(
                stepQuery.Where(s => s.StartTime >= startTime && s.StartTime <= endTime)
            );

            var opQuery = await _operationLogRepository.GetQueryableAsync();
            var manualInterventions = await AsyncExecuter.CountAsync(
                opQuery.Where(o => o.CreationTime >= startTime && o.CreationTime <= endTime && o.OperatorType == OperatorType.User)
            );

            var result = new TaskExecutionMetricsSummaryDto
            {
                StartTime = startTime,
                EndTime = endTime,
                FilterArea = input.Area,
                TotalTasks = tasks.Count,
                SucceededTasks = tasks.Count(t => t.Status == AgvTaskStatus.Succeeded),
                FailedTasks = tasks.Count(t => t.Status == AgvTaskStatus.Failed || t.Status == AgvTaskStatus.Canceled),
                ManualInterventionCount = manualInterventions
            };

            // 计算任务总耗时度量
            var taskDurations = tasks
                .Where(t => t.StartTime.HasValue && t.EndTime.HasValue)
                .Select(t => Math.Max(0, (long)(t.EndTime!.Value - t.StartTime!.Value).TotalMilliseconds))
                .ToList();
            result.TotalTaskDuration = CalculateDurationMetric(taskDurations);

            // 机械臂动作耗时 (Subsystem == Arm)
            var armDurations = steps
                .Where(s => string.Equals(s.Subsystem, ProfilingSubsystem.Arm, StringComparison.OrdinalIgnoreCase))
                .Select(s => s.DurationMs)
                .ToList();
            result.ArmActionDuration = CalculateDurationMetric(armDurations);

            // AGV 运动导航耗时 (Subsystem == TM 且 OperationName 包含 Move/Nav/Leg)
            var agvMoveDurations = steps
                .Where(s => string.Equals(s.Subsystem, ProfilingSubsystem.TM, StringComparison.OrdinalIgnoreCase)
                         && (s.OperationName.Contains("Move", StringComparison.OrdinalIgnoreCase)
                             || s.OperationName.Contains("Nav", StringComparison.OrdinalIgnoreCase)
                             || s.OperationName.Contains("Leg", StringComparison.OrdinalIgnoreCase)))
                .Select(s => s.DurationMs)
                .ToList();
            result.AgvMovementDuration = CalculateDurationMetric(agvMoveDurations);

            // 视觉二次定位含拍照 (Subsystem == Vision)
            var visionDurations = steps
                .Where(s => string.Equals(s.Subsystem, ProfilingSubsystem.Vision, StringComparison.OrdinalIgnoreCase))
                .Select(s => s.DurationMs)
                .ToList();
            result.VisualAlignDuration = CalculateDurationMetric(visionDurations);

            // 相互交管等待耗时 (Subsystem == TrafficControl)
            var trafficDurations = steps
                .Where(s => string.Equals(s.Subsystem, ProfilingSubsystem.TrafficControl, StringComparison.OrdinalIgnoreCase))
                .Select(s => s.DurationMs)
                .ToList();
            result.TrafficWaitDuration = CalculateDurationMetric(trafficDurations);

            // 各子系统平均耗时
            result.SubsystemAvgLatencies = steps
                .GroupBy(s => s.Subsystem)
                .ToDictionary(g => g.Key, g => Math.Round(g.Average(s => s.DurationMs), 2));

            // 计算整场平均稼动率估算
            var totalTimeSpanMs = (endTime - startTime).TotalMilliseconds;
            if (totalTimeSpanMs > 0 && tasks.Count > 0)
            {
                var totalWorkMs = taskDurations.Sum();
                var rate = Math.Min(100.0, Math.Round((totalWorkMs / totalTimeSpanMs) * 100.0, 2));
                result.AverageUtilizationRate = rate;
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<PagedResultDto<TaskStepProfilingDto>> GetStepListAsync(GetTaskStepListInput input)
        {
            var query = await _profilingRepository.GetQueryableAsync();

            if (!string.IsNullOrWhiteSpace(input.TaskCode))
            {
                query = query.Where(s => s.TaskCode == input.TaskCode);
            }
            if (!string.IsNullOrWhiteSpace(input.Subsystem))
            {
                query = query.Where(s => s.Subsystem == input.Subsystem);
            }
            if (!string.IsNullOrWhiteSpace(input.AgvId))
            {
                query = query.Where(s => s.AgvId == input.AgvId);
            }
            if (!string.IsNullOrWhiteSpace(input.Status))
            {
                query = query.Where(s => s.Status == input.Status);
            }
            if (input.StartTime.HasValue)
            {
                query = query.Where(s => s.StartTime >= input.StartTime.Value);
            }
            if (input.EndTime.HasValue)
            {
                query = query.Where(s => s.StartTime <= input.EndTime.Value);
            }

            var totalCount = await AsyncExecuter.CountAsync(query);

            var items = await AsyncExecuter.ToListAsync(
                query.OrderByDescending(s => s.StartTime)
                     .Skip(input.SkipCount)
                     .Take(input.MaxResultCount)
            );

            var dtos = items.Select(s => new TaskStepProfilingDto
            {
                Id = s.Id,
                TaskCode = s.TaskCode,
                BatchId = s.BatchId,
                AgvId = s.AgvId,
                StepIndex = s.StepIndex,
                ActiveLeg = s.ActiveLeg,
                Subsystem = s.Subsystem,
                OperationName = s.OperationName,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                DurationMs = s.DurationMs,
                Status = s.Status,
                Summary = s.Summary,
                Details = s.Details,
                TraceId = s.TraceId
            }).ToList();

            return new PagedResultDto<TaskStepProfilingDto>(totalCount, dtos);
        }

        private static List<TmStageNodeDto> BuildTmStages(AgvTask? task, List<TaskStepProfiling> steps)
        {
            var stages = new List<TmStageNodeDto>();
            var taskStatus = task?.Status ?? AgvTaskStatus.Pending;

            // 1. 创建
            stages.Add(new TmStageNodeDto
            {
                Stage = "创建",
                State = "Finished",
                Timestamp = task?.CreationTime ?? steps.FirstOrDefault()?.StartTime,
                DurationMs = 0
            });

            // 2. 已派发
            var dispatchStep = steps.FirstOrDefault(s => string.Equals(s.Subsystem, ProfilingSubsystem.TM, StringComparison.OrdinalIgnoreCase)
                                                      || string.Equals(s.Subsystem, ProfilingSubsystem.Dispatcher, StringComparison.OrdinalIgnoreCase));
            var isDispatched = taskStatus != AgvTaskStatus.Pending || dispatchStep != null;
            stages.Add(new TmStageNodeDto
            {
                Stage = "已派发",
                State = isDispatched ? "Finished" : "Pending",
                Timestamp = dispatchStep?.StartTime ?? task?.StartTime,
                DurationMs = dispatchStep?.DurationMs ?? 0
            });

            // 3. 取货 (Fetch)
            var fetchSteps = steps.Where(s => string.Equals(s.ActiveLeg, "Fetch", StringComparison.OrdinalIgnoreCase)
                                           || s.OperationName.Contains("Fetch", StringComparison.OrdinalIgnoreCase)
                                           || s.OperationName.Contains("Pick", StringComparison.OrdinalIgnoreCase)).ToList();
            var isFetching = string.Equals(task?.ActiveLeg, "Fetch", StringComparison.OrdinalIgnoreCase);
            var isFetchDone = task?.StepIndex > 1 || string.Equals(task?.ActiveLeg, "Put", StringComparison.OrdinalIgnoreCase) || taskStatus == AgvTaskStatus.Succeeded;
            stages.Add(new TmStageNodeDto
            {
                Stage = "取货",
                State = isFetchDone ? "Finished" : (isFetching ? "Running" : (isDispatched ? "Pending" : "Pending")),
                Timestamp = fetchSteps.FirstOrDefault()?.StartTime,
                DurationMs = fetchSteps.Sum(s => s.DurationMs)
            });

            // 4. 放货 (Put)
            var putSteps = steps.Where(s => string.Equals(s.ActiveLeg, "Put", StringComparison.OrdinalIgnoreCase)
                                         || s.OperationName.Contains("Put", StringComparison.OrdinalIgnoreCase)
                                         || s.OperationName.Contains("Place", StringComparison.OrdinalIgnoreCase)).ToList();
            var isPutting = string.Equals(task?.ActiveLeg, "Put", StringComparison.OrdinalIgnoreCase);
            var isPutDone = taskStatus == AgvTaskStatus.Succeeded;
            stages.Add(new TmStageNodeDto
            {
                Stage = "放货",
                State = isPutDone ? "Finished" : (isPutting ? "Running" : "Pending"),
                Timestamp = putSteps.FirstOrDefault()?.StartTime,
                DurationMs = putSteps.Sum(s => s.DurationMs)
            });

            // 5. 等待完成
            stages.Add(new TmStageNodeDto
            {
                Stage = "等待完成",
                State = taskStatus == AgvTaskStatus.Succeeded ? "Finished" : (taskStatus == AgvTaskStatus.Failed || taskStatus == AgvTaskStatus.Canceled ? "Failed" : "Pending"),
                Timestamp = task?.EndTime,
                DurationMs = 0
            });

            return stages;
        }

        private static List<UpstreamBatchNodeDto> BuildUpstreamBatches(AgvTask? task, List<TaskStepProfiling> steps)
        {
            var batches = new List<UpstreamBatchNodeDto>();

            var batchSteps = steps.Where(s => !string.IsNullOrWhiteSpace(s.BatchId)).ToList();
            if (batchSteps.Count > 0)
            {
                foreach (var group in batchSteps.GroupBy(s => s.BatchId))
                {
                    var isFailed = group.Any(s => string.Equals(s.Status, "Failed", StringComparison.OrdinalIgnoreCase));
                    batches.Add(new UpstreamBatchNodeDto
                    {
                        PlanId = group.Key!,
                        CarrierId = task?.CarrierCode,
                        Status = isFailed ? "失败" : "成功",
                        FailedReason = group.FirstOrDefault(s => s.Status == "Failed")?.Summary
                    });
                }
            }
            else if (!string.IsNullOrWhiteSpace(task?.BatchId))
            {
                batches.Add(new UpstreamBatchNodeDto
                {
                    PlanId = task.BatchId,
                    CarrierId = task.CarrierCode,
                    Status = task.Status == AgvTaskStatus.Succeeded ? "成功" : (task.Status == AgvTaskStatus.Failed ? "失败" : "进行中"),
                    FailedReason = task.FailureReason
                });
            }

            return batches;
        }

        private static TaskMetricsBreakdownDto BuildMetricsBreakdown(long totalDurationMs, List<TaskStepProfiling> steps)
        {
            var ama = steps.Where(s => string.Equals(s.Subsystem, ProfilingSubsystem.AMA, StringComparison.OrdinalIgnoreCase)).Sum(s => s.DurationMs);
            var mica = steps.Where(s => string.Equals(s.Subsystem, ProfilingSubsystem.Mica, StringComparison.OrdinalIgnoreCase)).Sum(s => s.DurationMs);
            var plc = steps.Where(s => string.Equals(s.Subsystem, ProfilingSubsystem.PLC, StringComparison.OrdinalIgnoreCase)).Sum(s => s.DurationMs);
            var locker = steps.Where(s => string.Equals(s.Subsystem, ProfilingSubsystem.LocationLock, StringComparison.OrdinalIgnoreCase)).Sum(s => s.DurationMs);
            var tm = steps.Where(s => string.Equals(s.Subsystem, ProfilingSubsystem.TM, StringComparison.OrdinalIgnoreCase)).Sum(s => s.DurationMs);
            var arm = steps.Where(s => string.Equals(s.Subsystem, ProfilingSubsystem.Arm, StringComparison.OrdinalIgnoreCase)).Sum(s => s.DurationMs);
            var vision = steps.Where(s => string.Equals(s.Subsystem, ProfilingSubsystem.Vision, StringComparison.OrdinalIgnoreCase)).Sum(s => s.DurationMs);
            var traffic = steps.Where(s => string.Equals(s.Subsystem, ProfilingSubsystem.TrafficControl, StringComparison.OrdinalIgnoreCase)).Sum(s => s.DurationMs);

            var knownSum = ama + mica + plc + locker + tm + arm + vision + traffic;
            var other = Math.Max(0, totalDurationMs - knownSum);

            return new TaskMetricsBreakdownDto
            {
                TotalDurationMs = totalDurationMs,
                AmaInteractionMs = ama,
                MicaInteractionMs = mica,
                PlcInteractionMs = plc,
                LocationLockMs = locker,
                TmDispatchMs = tm,
                AgvNavDurationMs = tm,
                ArmActionDurationMs = arm,
                VisualAlignDurationMs = vision,
                TrafficWaitDurationMs = traffic,
                OtherMs = other
            };
        }

        private static DurationMetricDto CalculateDurationMetric(List<long> values)
        {
            if (values == null || values.Count == 0)
            {
                return new DurationMetricDto();
            }

            var sorted = values.OrderBy(v => v).ToList();
            var count = sorted.Count;
            var p95Index = (int)Math.Ceiling(0.95 * count) - 1;
            p95Index = Math.Clamp(p95Index, 0, count - 1);

            return new DurationMetricDto
            {
                SampleCount = count,
                AverageMs = Math.Round(sorted.Average(), 2),
                MinMs = sorted.First(),
                MaxMs = sorted.Last(),
                P95Ms = sorted[p95Index]
            };
        }
    }
}
