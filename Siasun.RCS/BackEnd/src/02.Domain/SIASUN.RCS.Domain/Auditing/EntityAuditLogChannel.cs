using System.Threading.Channels;

namespace SIASUN.RCS.Auditing
{
    public class EntityAuditLogChannel
    {
        private readonly Channel<EntityAuditLogEntry> _channel;

        public EntityAuditLogChannel()
        {
            var options = new BoundedChannelOptions(20000)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            };
            
            _channel = Channel.CreateBounded<EntityAuditLogEntry>(options);
        }

        public bool TryWrite(EntityAuditLogEntry entry)
        {
            return _channel.Writer.TryWrite(entry);
        }

        public ChannelReader<EntityAuditLogEntry> Reader => _channel.Reader;
    }
}
