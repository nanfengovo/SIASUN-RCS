using System.Threading.Channels;
using SIASUN.RCS.Auditing;

namespace SIASUN.RCS.Infrastructure.Logging
{
    public class ApiAuditLogChannel
    {
        private readonly Channel<ApiAuditLogEntry> _channel;

        public ApiAuditLogChannel()
        {
            var options = new BoundedChannelOptions(20000)
            {
                FullMode = BoundedChannelFullMode.DropOldest, // 当通道满时，丢弃最旧的日志条目
                SingleReader = true,
                SingleWriter = false
            };
            _channel = Channel.CreateBounded<ApiAuditLogEntry>(options);
        }

        public bool TryWrite(ApiAuditLogEntry entry) =>
        _channel.Writer.TryWrite(entry);
        public ChannelReader<ApiAuditLogEntry> Reader => _channel.Reader;
    }
}