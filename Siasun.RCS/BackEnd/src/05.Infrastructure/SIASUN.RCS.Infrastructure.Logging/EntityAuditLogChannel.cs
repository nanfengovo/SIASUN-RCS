using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Diagnostics;
using SIASUN.RCS.Infrastructure.Logging.Channels;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Infrastructure.Logging
{
    /// <summary>
    /// 实体变更审计日志异步通道管理器（单例）
    /// 采用双轨有界设计：AgvTask、AgvVehicle 与 OperationLog 等核心领域实体变更走特权通道（Wait 模式 + Spill 溢出保全），高频常规实体走保盘通道（DropOldest 模式）
    /// </summary>
    public class EntityAuditLogChannel : IEntityAuditLogChannel, ISingletonDependency
    {
        private readonly Channel<EntityAuditLogMessage> _priorityChannel;
        private readonly Channel<EntityAuditLogMessage> _normalChannel;
        private readonly PriorityChannelReader<EntityAuditLogMessage> _reader;
        private readonly EvidenceSpillBuffer<EntityAuditLogMessage> _spillBuffer;
        private readonly IEvidencePrivilegePolicy _privilegePolicy;

        /// <summary>
        /// 特权铁证累计溢出保全条目总数（大于 0 说明发生过极端通道饱和并触发保全）
        /// </summary>
        public long SpillCount => _spillBuffer.TotalSpillCount;

        /// <summary>
        /// 当前待消费的紧急溢出保全事件数量
        /// </summary>
        public int PendingSpillCount => _spillBuffer.PendingSpillCount;

        /// <summary>
        /// 累计应急落盘本地磁盘写入失败次数（大于 0 意味着磁盘写保护或 I/O 故障）
        /// </summary>
        public long SpillDiskWriteFailures => _spillBuffer.SpillDiskWriteFailures;

        /// <summary>
        /// 特权通道当前堆积队列深度
        /// </summary>
        public int PriorityQueueCount => _priorityChannel.Reader.Count;

        /// <summary>
        /// 常规通道当前堆积队列深度
        /// </summary>
        public int NormalQueueCount => _normalChannel.Reader.Count;

        /// <summary>
        /// 综合通道总等待深度（含特权、常规与溢出环待消费数）
        /// </summary>
        public int TotalQueueCount => _reader.Count;

        /// <summary>
        /// 特权溢出保全缓冲区实例
        /// </summary>
        public EvidenceSpillBuffer<EntityAuditLogMessage> SpillBuffer => _spillBuffer;

        /// <summary>
        /// 从本地磁盘回放恢复未入库的溢出日志
        /// </summary>
        /// <returns>恢复条目数</returns>
        public int RecoverDiskSpills() => _spillBuffer.RecoverDiskSpills();

        /// <summary>
        /// 默认构造函数，初始化双轨实体变更审计通道与特权溢出保全环
        /// </summary>
        /// <param name="privilegePolicy">特权证据策略（可选，默认为 DefaultEvidencePrivilegePolicy）</param>
        /// <param name="spillDir">紧急溢出落盘目录（可选）</param>
        public EntityAuditLogChannel(IEvidencePrivilegePolicy? privilegePolicy = null, string? spillDir = null)
        {
            _privilegePolicy = privilegePolicy ?? DefaultEvidencePrivilegePolicy.Instance;
            _spillBuffer = new EvidenceSpillBuffer<EntityAuditLogMessage>("entity_audit", spillDir);

            var priorityOptions = new BoundedChannelOptions(10000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            };
            _priorityChannel = Channel.CreateBounded<EntityAuditLogMessage>(priorityOptions);

            var normalOptions = new BoundedChannelOptions(20000)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            };
            _normalChannel = Channel.CreateBounded<EntityAuditLogMessage>(normalOptions);

            _reader = new PriorityChannelReader<EntityAuditLogMessage>(_priorityChannel.Reader, _normalChannel.Reader, _spillBuffer);
        }

        /// <summary>
        /// 尝试向通道写入一条实体变更消息，根据实体重要度自动路由至特权或常规通道
        /// 核心领域实体变更（AgvTask、AgvVehicle、OperationLog）排队满载时自动短暂等待槽位；若依然拥堵，无缝推入溢出环紧急落盘保全，绝不静默丢失
        /// </summary>
        /// <param name="message">实体变更审计消息</param>
        /// <returns>是否成功入队或保全</returns>
        public bool TryWrite(EntityAuditLogMessage message)
        {
            if (message == null) return false;

            if (IsPrivilegedEntity(message.EntityName))
            {
                if (_priorityChannel.Writer.TryWrite(message))
                {
                    return true;
                }

                // 特权通道满载时进行短暂等待，保障核心实体不可抵赖审计证据
                try
                {
                    var writeTask = _priorityChannel.Writer.WriteAsync(message).AsTask();
                    if (writeTask.Wait(TimeSpan.FromSeconds(1)))
                    {
                        return true;
                    }
                }
                catch
                {
                    // 吞掉等待异常，进入最终 Spill 保全
                }

                // 极端拥堵时激活特权铁证溢出保全环，零静默丢弃
                _spillBuffer.Enqueue(message);
                return true;
            }

            return _normalChannel.Writer.TryWrite(message);
        }

        /// <summary>
        /// 异步向通道写入一条实体变更消息，核心实体变更在缓冲区满时将异步等待槽位；若发生意外取消，自动推入溢出环紧急保全
        /// </summary>
        /// <param name="message">实体变更审计消息</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async ValueTask WriteAsync(EntityAuditLogMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null) return;

            if (IsPrivilegedEntity(message.EntityName))
            {
                try
                {
                    await _priorityChannel.Writer.WriteAsync(message, cancellationToken);
                }
                catch
                {
                    // 异步被取消或异常时，执行紧急溢出保全
                    _spillBuffer.Enqueue(message);
                }
                return;
            }

            _normalChannel.Writer.TryWrite(message);
        }

        /// <summary>
        /// 获取双轨优先读取器（优先消费溢出环与核心聚合根实体变更）
        /// </summary>
        public ChannelReader<EntityAuditLogMessage> Reader => _reader;

        /// <summary>
        /// 判定实体名是否属于关键核心特权实体（如 AgvTask, AgvVehicle, Operation 等）
        /// </summary>
        /// <param name="entityName">实体名称</param>
        /// <returns>是否属于特权实体</returns>
        public bool IsPrivilegedEntity(string? entityName)
        {
            return _privilegePolicy.IsPrivilegedCategory(entityName);
        }
    }
}
