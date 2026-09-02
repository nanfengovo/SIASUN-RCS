using System.Threading.Channels;
using SIASUN.RCS.Logs.OperatorLogs;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Infrastructure.Logging.OperationLogs
{
    public class OperationLogChannelManager : ISingletonDependency
    {
        public Channel<OperationLog> Channel { get; }

        public OperationLogChannelManager()
        {
            // 限制最大容量，防止写库瘫痪导致内存OOM
            Channel = System.Threading.Channels.Channel.CreateBounded<OperationLog>(new BoundedChannelOptions(10000)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });
        }
    }
}