using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Infrastructure.Logging.Channels;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Infrastructure.Logging
{
    /// <summary>
    /// 实体变更审计日志异步通道管理器（单例）
    /// 采用双轨有界设计：AgvTask、AgvVehicle 与 OperationLog 等核心领域实体变更走特权通道（Wait 模式），高频常规实体走保盘通道（DropOldest 模式）
    /// </summary>
    public class EntityAuditLogChannel : IEntityAuditLogChannel, ISingletonDependency
    {
        private readonly Channel<EntityAuditLogMessage> _priorityChannel;
        private readonly Channel<EntityAuditLogMessage> _normalChannel;
        private readonly ChannelReader<EntityAuditLogMessage> _reader;

        /// <summary>
        /// 默认构造函数，初始化双轨实体变更审计通道
        /// </summary>
        public EntityAuditLogChannel()
        {
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

            _reader = new PriorityChannelReader<EntityAuditLogMessage>(_priorityChannel.Reader, _normalChannel.Reader);
        }

        /// <summary>
        /// 尝试向通道写入一条实体变更消息，根据实体重要度自动路由至特权或常规通道
        /// 核心领域实体变更（AgvTask、AgvVehicle、OperationLog）排队满载时自动阻塞等待（至多 2 秒），绝不静默丢失
        /// </summary>
        /// <param name="message">实体变更审计消息</param>
        /// <returns>是否成功入队</returns>
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
                    return writeTask.Wait(TimeSpan.FromSeconds(2));
                }
                catch
                {
                    return false;
                }
            }

            return _normalChannel.Writer.TryWrite(message);
        }

        /// <summary>
        /// 异步向通道写入一条实体变更消息，核心实体变更在缓冲区满时将异步等待槽位，绝对不丢
        /// </summary>
        /// <param name="message">实体变更审计消息</param>
        /// <param name="cancellationToken">取消令牌</param>
        public ValueTask WriteAsync(EntityAuditLogMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null) return ValueTask.CompletedTask;

            if (IsPrivilegedEntity(message.EntityName))
            {
                return _priorityChannel.Writer.WriteAsync(message, cancellationToken);
            }

            _normalChannel.Writer.TryWrite(message);
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// 获取双轨优先读取器（优先消费核心聚合根实体变更）
        /// </summary>
        public ChannelReader<EntityAuditLogMessage> Reader => _reader;

        private static bool IsPrivilegedEntity(string? entityName)
        {
            if (string.IsNullOrEmpty(entityName)) return false;

            return string.Equals(entityName, "AgvTask", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(entityName, "AgvVehicle", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(entityName, "OperationLog", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(entityName, "AppOperationLogs", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(entityName, "AppAgvTasks", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(entityName, "AppAgvVehicles", StringComparison.OrdinalIgnoreCase);
        }
    }
}
