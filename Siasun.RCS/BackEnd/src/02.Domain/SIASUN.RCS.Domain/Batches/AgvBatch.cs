using System;
using SIASUN.RCS.Batches.Events;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace SIASUN.RCS.Batches
{
    /// <summary>
    /// AGV 批次管理聚合根（支持一单分拆多子任务、多车协同与载具编排）
    /// 遵循《AGENTS.md》铁律 6：领域模型原生支持批次分解与多车安全互锁编排
    /// </summary>
    public class AgvBatch : FullAuditedAggregateRoot<Guid>
    {
        /// <summary>
        /// 批次唯一编码（例如 "BATCH_20260908_001"）
        /// </summary>
        public string BatchCode { get; private set; } = string.Empty;

        /// <summary>
        /// 批次流转状态
        /// </summary>
        public AgvBatchStatus Status { get; private set; } = AgvBatchStatus.Created;

        /// <summary>
        /// 拆分后的总子任务数量
        /// </summary>
        public int TotalSubTasks { get; private set; }

        /// <summary>
        /// 已成功完成的子任务数量
        /// </summary>
        public int CompletedSubTasks { get; private set; }

        /// <summary>
        /// 失败的子任务数量
        /// </summary>
        public int FailedSubTasks { get; private set; }

        /// <summary>
        /// 包含的载具/FOUP 晶圆盒编号清单（逗号分隔或 JSON）
        /// </summary>
        public string CarrierCodes { get; private set; } = string.Empty;

        /// <summary>
        /// 批次物料起点工位
        /// </summary>
        public string SourceStation { get; private set; } = string.Empty;

        /// <summary>
        /// 批次物料终点工位
        /// </summary>
        public string TargetStation { get; private set; } = string.Empty;

        /// <summary>
        /// 全链路追踪 TraceId
        /// </summary>
        public string? TraceId { get; private set; }

        /// <summary>
        /// 批次描述或备注
        /// </summary>
        public string? Remark { get; private set; }

        /// <summary>
        /// EF Core 反序列化构造函数
        /// </summary>
        protected AgvBatch()
        {
        }

        /// <summary>
        /// 创建新批次订单
        /// </summary>
        public AgvBatch(
            Guid id,
            string batchCode,
            string sourceStation,
            string targetStation,
            string? carrierCodes = null,
            string? traceId = null,
            string? remark = null) : base(id)
        {
            BatchCode = Check.NotNullOrWhiteSpace(batchCode, nameof(batchCode));
            SourceStation = Check.NotNullOrWhiteSpace(sourceStation, nameof(sourceStation));
            TargetStation = Check.NotNullOrWhiteSpace(targetStation, nameof(targetStation));
            CarrierCodes = carrierCodes ?? string.Empty;
            TraceId = traceId;
            Remark = remark;
            Status = AgvBatchStatus.Created;
            TotalSubTasks = 0;
            CompletedSubTasks = 0;
            FailedSubTasks = 0;
        }

        /// <summary>
        /// 设定批次拆单参数
        /// </summary>
        /// <param name="totalCount">子任务总数</param>
        /// <param name="carrierCodes">载具清单</param>
        public void ConfigureSubTasks(int totalCount, string carrierCodes)
        {
            if (totalCount <= 0)
            {
                throw new BusinessException("BATCH_TASK_COUNT_INVALID", "批次子任务数量必须大于 0。");
            }

            TotalSubTasks = totalCount;
            CarrierCodes = carrierCodes;
            Status = AgvBatchStatus.Dispatching;

            AddDistributedEvent(new BatchStatusChangedEvent(
                Id,
                BatchCode,
                AgvBatchStatus.Created,
                AgvBatchStatus.Dispatching,
                CompletedSubTasks,
                TotalSubTasks,
                "批次拆分子任务已就绪并开始派发",
                TraceId));
        }

        /// <summary>
        /// 记录某子任务顺利完成并检查汇聚同步
        /// </summary>
        /// <param name="carrierCode">完成的载具编号</param>
        public void RecordSubTaskCompleted(string carrierCode)
        {
            if (Status != AgvBatchStatus.Dispatching && Status != AgvBatchStatus.PartiallyCompleted)
            {
                return;
            }

            var oldStatus = Status;
            CompletedSubTasks++;

            if (CompletedSubTasks >= TotalSubTasks && TotalSubTasks > 0)
            {
                Status = AgvBatchStatus.Completed;
            }
            else
            {
                Status = AgvBatchStatus.PartiallyCompleted;
            }

            AddDistributedEvent(new BatchStatusChangedEvent(
                Id,
                BatchCode,
                oldStatus,
                Status,
                CompletedSubTasks,
                TotalSubTasks,
                $"子任务载具 [{carrierCode}] 搬运完成",
                TraceId));
        }

        /// <summary>
        /// 记录某子任务发生不可恢复失败
        /// </summary>
        /// <param name="carrierCode">失败的载具编号</param>
        /// <param name="reason">失败根因</param>
        public void RecordSubTaskFailed(string carrierCode, string reason)
        {
            var oldStatus = Status;
            FailedSubTasks++;
            Status = AgvBatchStatus.Failed;

            AddDistributedEvent(new BatchStatusChangedEvent(
                Id,
                BatchCode,
                oldStatus,
                AgvBatchStatus.Failed,
                CompletedSubTasks,
                TotalSubTasks,
                $"子任务载具 [{carrierCode}] 搬运失败: {reason}",
                TraceId));
        }

        /// <summary>
        /// 调度员或上游系统主动取消批次
        /// </summary>
        /// <param name="reason">取消原因</param>
        public void Cancel(string reason)
        {
            if (Status == AgvBatchStatus.Completed)
            {
                throw new BusinessException("BATCH_ALREADY_COMPLETED", "已完成的批次不允许取消。");
            }

            var oldStatus = Status;
            Status = AgvBatchStatus.Canceled;
            Remark = $"取消原因: {reason}; {Remark}";

            AddDistributedEvent(new BatchStatusChangedEvent(
                Id,
                BatchCode,
                oldStatus,
                AgvBatchStatus.Canceled,
                CompletedSubTasks,
                TotalSubTasks,
                reason,
                TraceId));
        }
    }
}
