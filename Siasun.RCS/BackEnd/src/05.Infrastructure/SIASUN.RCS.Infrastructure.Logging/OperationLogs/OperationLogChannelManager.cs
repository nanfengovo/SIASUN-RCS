using System.Threading.Channels;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Infrastructure.Logging.Channels;
using SIASUN.RCS.Logs.OperatorLogs;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Infrastructure.Logging.OperationLogs
{
    /// <summary>
    /// 调度员操作与系统自审计日志内存通道管理器（单例）
    /// 严格承载第 2 层不可抵赖铁证，采用有界等待模式（Wait）与特权溢出保全（SpillBuffer），杜绝人为操作记录静默丢失
    /// </summary>
    [ExposeServices(typeof(OperationLogChannelManager), typeof(IOperationLogChannel))]
    public class OperationLogChannelManager : IOperationLogChannel, ISingletonDependency
    {
        /// <summary>
        /// 操作审计异步管道实例
        /// </summary>
        public Channel<OperationLog> Channel { get; }

        /// <summary>
        /// 特权铁证紧急溢出保全环缓冲区实例
        /// </summary>
        public EvidenceSpillBuffer<OperationLog> SpillBuffer { get; }

        /// <summary>
        /// 累计特权溢出保全总次数
        /// </summary>
        public long SpillCount => SpillBuffer.TotalSpillCount;

        /// <summary>
        /// 当前待消费的特权溢出条目数
        /// </summary>
        public int PendingSpillCount => SpillBuffer.PendingSpillCount;

        /// <summary>
        /// 累计应急落盘本地磁盘写入失败次数（大于 0 意味着磁盘写保护或 I/O 故障）
        /// </summary>
        public long SpillDiskWriteFailures => SpillBuffer.SpillDiskWriteFailures;

        /// <summary>
        /// 综合队列当前积压深度（含通道内部积压与未消费溢出环条目数）
        /// </summary>
        public int TotalQueueCount => Channel.Reader.Count + SpillBuffer.PendingSpillCount;

        /// <summary>
        /// 默认构造函数，初始化具备背压保护与溢出保全的不可抵赖操作审计通道
        /// </summary>
        /// <param name="spillDir">磁盘应急落盘目录（可选）</param>
        public OperationLogChannelManager(string? spillDir = null)
        {
            SpillBuffer = new EvidenceSpillBuffer<OperationLog>("operation_log", spillDir);
            Channel = System.Threading.Channels.Channel.CreateBounded<OperationLog>(new BoundedChannelOptions(10000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });
        }

        /// <summary>
        /// 从本地磁盘回放未消费的溢出操作日志
        /// </summary>
        /// <returns>回放恢复的条目数</returns>
        public int RecoverDiskSpills() => SpillBuffer.RecoverDiskSpills();
    }
}