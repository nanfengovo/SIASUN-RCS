using System;
using System.Threading.Channels;
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
        /// </summary>
        /// <param name="message">实体变更审计消息</param>
        /// <returns>是否成功入队</returns>
        public bool TryWrite(EntityAuditLogMessage message)
        {
            if (message == null) return false;

            if (IsPrivilegedEntity(message.EntityName))
            {
                return _priorityChannel.Writer.TryWrite(message);
            }

            return _normalChannel.Writer.TryWrite(message);
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
