using System.Threading.Channels;
using SIASUN.RCS.Auditing;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Infrastructure.Logging
{
    public class EntityAuditLogChannel : IEntityAuditLogChannel, ISingletonDependency
    {
        private readonly Channel<EntityAuditLogMessage> _channel;

        public EntityAuditLogChannel()
        {
            var options = new BoundedChannelOptions(20000)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            };
            _channel = Channel.CreateBounded<EntityAuditLogMessage>(options);
        }

        public bool TryWrite(EntityAuditLogMessage message)
        {
            return _channel.Writer.TryWrite(message);
        }

        public ChannelReader<EntityAuditLogMessage> Reader => _channel.Reader;
    }
}
