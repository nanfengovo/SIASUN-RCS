using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace SIASUN.RCS.Batches
{
    /// <summary>
    /// 批次管理与多车编排领域服务实现
    /// 遵循《AGENTS.md》铁律 6：支持一单分拆多子任务、载具绑定（Carrier）与多车汇聚协同
    /// </summary>
    public class BatchOrchestrator : IBatchOrchestrator, ITransientDependency
    {
        private readonly IRepository<AgvTask, Guid> _taskRepository;
        private readonly IGuidGenerator _guidGenerator;
        private readonly ILogger<BatchOrchestrator> _logger;

        public BatchOrchestrator(
            IRepository<AgvTask, Guid> taskRepository,
            IGuidGenerator guidGenerator,
            ILogger<BatchOrchestrator> logger)
        {
            _taskRepository = taskRepository;
            _guidGenerator = guidGenerator;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<AgvTask>> DecomposeBatchAsync(
            AgvBatch batch,
            IReadOnlyList<string> carrierCodes,
            string workflowKey = "transfer_standard",
            CancellationToken cancellationToken = default)
        {
            Check.NotNull(batch, nameof(batch));
            Check.NotNull(carrierCodes, nameof(carrierCodes));

            if (carrierCodes.Count == 0)
            {
                throw new BusinessException("BATCH_NO_CARRIERS", "无法编排空载具批次，载具清单不能为空。");
            }

            _logger.LogInformation("正在将批次 [{BatchCode}] 拆分为 {Count} 个独立单车搬运任务...",
                batch.BatchCode, carrierCodes.Count);

            var createdTasks = new List<AgvTask>();

            for (var i = 0; i < carrierCodes.Count; i++)
            {
                var carrier = carrierCodes[i];
                var subTaskCode = $"{batch.BatchCode}_{i + 1:D2}_{carrier}";

                var task = new AgvTask(
                    _guidGenerator.Create(),
                    subTaskCode,
                    fromStation: batch.SourceStation,
                    toStation: batch.TargetStation,
                    carrierCode: carrier,
                    batchId: batch.BatchCode,
                    traceId: batch.TraceId);

                task.SetWorkflowKey(workflowKey);

                await _taskRepository.InsertAsync(task, autoSave: true, cancellationToken: cancellationToken);
                createdTasks.Add(task);
            }

            // 更新批次聚合根状态
            batch.ConfigureSubTasks(carrierCodes.Count, string.Join(",", carrierCodes));

            _logger.LogInformation("批次 [{BatchCode}] 成功拆分 {Count} 个子任务，状态转入 Dispatching。",
                batch.BatchCode, createdTasks.Count);

            return createdTasks;
        }

        /// <inheritdoc />
        public async Task<bool> EvaluateConvergenceAsync(
            AgvBatch batch,
            CancellationToken cancellationToken = default)
        {
            Check.NotNull(batch, nameof(batch));

            var subTasks = await _taskRepository.GetListAsync(
                t => t.BatchId == batch.BatchCode,
                cancellationToken: cancellationToken);

            if (subTasks.Count == 0)
            {
                return false;
            }

            // 1. 检查是否存在失败子任务
            var failedTask = subTasks.FirstOrDefault(t => t.Status == AgvTaskStatus.Failed);
            if (failedTask != null)
            {
                _logger.LogWarning("批次 [{BatchCode}] 发现子任务 [{TaskCode}] 执行失败，批次标记失败。",
                    batch.BatchCode, failedTask.TaskCode);

                batch.RecordSubTaskFailed(failedTask.CarrierCode ?? "UNKNOWN", "子任务执行失败");
                return false;
            }

            // 2. 统计已成功完成的任务
            var completedTasks = subTasks.Where(t => t.Status == AgvTaskStatus.Succeeded).ToList();
            if (completedTasks.Count == subTasks.Count && subTasks.Count > 0)
            {
                foreach (var t in completedTasks)
                {
                    batch.RecordSubTaskCompleted(t.CarrierCode ?? string.Empty);
                }

                _logger.LogInformation("批次 [{BatchCode}] 所有 {Count} 个子任务均已完成，批次汇聚同步达成！",
                    batch.BatchCode, subTasks.Count);

                return true;
            }

            return false;
        }
    }
}
