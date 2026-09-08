using System.Threading.Channels;
using SIASUN.RCS.Tasks.Profiling;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Infrastructure.Logging.Profiling
{
    /// <summary>
    /// 任务步骤剖析高性能无锁通道
    /// 隔离调度执行主链路与数据库 I/O，保障核心调度线程零阻塞
    /// </summary>
    public class TaskProfilingChannel : ISingletonDependency
    {
        private readonly Channel<TaskStepProfiling> _channel;

        /// <summary>
        /// 通道读取端
        /// </summary>
        public ChannelReader<TaskStepProfiling> Reader => _channel.Reader;

        /// <summary>
        /// 通道写入端
        /// </summary>
        public ChannelWriter<TaskStepProfiling> Writer => _channel.Writer;

        /// <summary>
        /// 初始化容量为 10000 的有界通道，背压时丢弃最旧数据并记录指标
        /// </summary>
        public TaskProfilingChannel()
        {
            var options = new BoundedChannelOptions(10000)
            {
                SingleWriter = false,
                SingleReader = true,
                FullMode = BoundedChannelFullMode.DropOldest
            };
            _channel = Channel.CreateBounded<TaskStepProfiling>(options);
        }
    }
}
