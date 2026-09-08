using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Batches.Dtos;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace SIASUN.RCS.Batches
{
    /// <summary>
    /// AGV 批次管理与多车编排应用服务实现
    /// 遵循《AGENTS.md》铁律 6：领域模型原生支持批次分解与多车协同编排
    /// </summary>
    public class BatchAppService : ApplicationService, IBatchAppService
    {
        private readonly IRepository<AgvBatch, Guid> _batchRepository;
        private readonly IBatchOrchestrator _batchOrchestrator;

        public BatchAppService(
            IRepository<AgvBatch, Guid> batchRepository,
            IBatchOrchestrator batchOrchestrator)
        {
            _batchRepository = batchRepository;
            _batchOrchestrator = batchOrchestrator;
        }

        /// <inheritdoc />
        public async Task<AgvBatchDto> CreateAndDecomposeBatchAsync(CreateBatchDto input)
        {
            Check.NotNull(input, nameof(input));
            Check.NotNullOrWhiteSpace(input.BatchCode, nameof(input.BatchCode));

            // 1. 批次号防重排重
            var existing = await _batchRepository.FirstOrDefaultAsync(b => b.BatchCode == input.BatchCode);
            if (existing != null)
            {
                throw new BusinessException("BATCH_ALREADY_EXISTS", $"批次编号 [{input.BatchCode}] 已存在，禁止重复创建。");
            }

            // 2. 创建批次聚合根
            var batch = new AgvBatch(
                GuidGenerator.Create(),
                input.BatchCode,
                input.SourceStation,
                input.TargetStation,
                string.Join(",", input.CarrierCodes),
                input.TraceId,
                input.Remark);

            await _batchRepository.InsertAsync(batch, autoSave: true);

            // 3. 驱动领域编排服务将批次拆分为单车搬运任务
            await _batchOrchestrator.DecomposeBatchAsync(
                batch,
                input.CarrierCodes,
                input.WorkflowKey);

            await _batchRepository.UpdateAsync(batch, autoSave: true);

            Logger.LogInformation("批次 [{BatchCode}] 已创建并成功编排拆分 {Count} 个子任务。",
                batch.BatchCode, batch.TotalSubTasks);

            return MapToDto(batch);
        }

        /// <inheritdoc />
        public async Task<AgvBatchDto> GetAsync(Guid id)
        {
            var batch = await _batchRepository.GetAsync(id);
            return MapToDto(batch);
        }

        /// <inheritdoc />
        public async Task CancelAsync(Guid id, string reason)
        {
            Check.NotNullOrWhiteSpace(reason, nameof(reason));

            var batch = await _batchRepository.GetAsync(id);
            batch.Cancel(reason);

            await _batchRepository.UpdateAsync(batch, autoSave: true);
            Logger.LogWarning("调度员人工取消批次 [{BatchCode}], 原因: {Reason}", batch.BatchCode, reason);
        }

        private static AgvBatchDto MapToDto(AgvBatch batch)
        {
            return new AgvBatchDto
            {
                Id = batch.Id,
                BatchCode = batch.BatchCode,
                Status = batch.Status,
                TotalSubTasks = batch.TotalSubTasks,
                CompletedSubTasks = batch.CompletedSubTasks,
                FailedSubTasks = batch.FailedSubTasks,
                CarrierCodes = batch.CarrierCodes,
                SourceStation = batch.SourceStation,
                TargetStation = batch.TargetStation,
                TraceId = batch.TraceId,
                Remark = batch.Remark,
                CreationTime = batch.CreationTime,
                CreatorId = batch.CreatorId,
                LastModificationTime = batch.LastModificationTime,
                LastModifierId = batch.LastModifierId
            };
        }
    }
}
