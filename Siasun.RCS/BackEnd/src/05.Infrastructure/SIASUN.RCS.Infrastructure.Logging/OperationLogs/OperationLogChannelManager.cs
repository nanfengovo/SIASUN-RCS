using System.Threading.Channels;
using SIASUN.RCS.Logs.OperatorLogs;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Infrastructure.Logging.OperationLogs
{
    /// <summary>
    /// 调度员操作与系统自审计日志内存通道管理器（单例）
    /// 严格承载第 2 层不可抵赖铁证，采用有界等待模式（Wait）与背压保护，杜绝人为操作记录静默丢失
    /// </summary>
    public class OperationLogChannelManager : ISingletonDependency
    {
        /// <summary>
        /// 操作审计异步管道实例
        /// </summary>
        public Channel<OperationLog> Channel { get; }

        /// <summary>
        /// 默认构造函数，初始化具备背压保护的不可抵赖操作审计通道
        /// </summary>
        public OperationLogChannelManager()
        {
            Channel = System.Threading.Channels.Channel.CreateBounded<OperationLog>(new BoundedChannelOptions(10000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });
        }
    }
}