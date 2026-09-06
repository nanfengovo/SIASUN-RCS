using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Infrastructure.Logging.Channels;

namespace SIASUN.RCS.Infrastructure.Logging
{
    /// <summary>
    /// API 接口报文审计异步管道管理器
    /// 采用双轨有界队列设计：特权异常与调度报文走不可丢弃通道（Wait 模式），常规心跳与成功报文走保盘降采样通道（DropOldest 模式）
    /// </summary>
    public class ApiAuditLogChannel
    {
        private readonly Channel<ApiAuditLogEntry> _priorityChannel;
        private readonly Channel<ApiAuditLogEntry> _normalChannel;
        private readonly ChannelReader<ApiAuditLogEntry> _reader;

        /// <summary>
        /// 默认构造函数，初始化双轨 API 报文审计通道
        /// </summary>
        public ApiAuditLogChannel()
        {
            var priorityOptions = new BoundedChannelOptions(10000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            };
            _priorityChannel = Channel.CreateBounded<ApiAuditLogEntry>(priorityOptions);

            var normalOptions = new BoundedChannelOptions(20000)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            };
            _normalChannel = Channel.CreateBounded<ApiAuditLogEntry>(normalOptions);

            _reader = new PriorityChannelReader<ApiAuditLogEntry>(_priorityChannel.Reader, _normalChannel.Reader);
        }

        /// <summary>
        /// 尝试向通道写入一条 API 报文条目，根据优先级自动路由至特权或常规通道
        /// 针对特权异常与调度接口铁证，排队满载时自动阻塞等待槽位（至多 2 秒），坚决杜绝静默丢弃
        /// </summary>
        /// <param name="entry">报文审计条目</param>
        /// <returns>是否成功入队</returns>
        public bool TryWrite(ApiAuditLogEntry entry)
        {
            if (entry == null) return false;

            if (IsPrivilegedEntry(entry))
            {
                if (_priorityChannel.Writer.TryWrite(entry))
                {
                    return true;
                }

                // 特权通道虽然满了，由于 FullMode=Wait，TryWrite 会立即返回 false。
                // 为达成 L4 铁证零丢失，特权条目进行阻塞写入等待（至多 2 秒）
                try
                {
                    var writeTask = _priorityChannel.Writer.WriteAsync(entry).AsTask();
                    return writeTask.Wait(TimeSpan.FromSeconds(2));
                }
                catch
                {
                    return false;
                }
            }

            return _normalChannel.Writer.TryWrite(entry);
        }

        /// <summary>
        /// 异步向通道写入一条 API 报文条目，特权异常条目在缓冲区满时将异步等待槽位，绝对不丢
        /// </summary>
        /// <param name="entry">报文审计条目</param>
        /// <param name="cancellationToken">取消令牌</param>
        public ValueTask WriteAsync(ApiAuditLogEntry entry, CancellationToken cancellationToken = default)
        {
            if (entry == null) return ValueTask.CompletedTask;

            if (IsPrivilegedEntry(entry))
            {
                return _priorityChannel.Writer.WriteAsync(entry, cancellationToken);
            }

            _normalChannel.Writer.TryWrite(entry);
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// 获取双轨优先读取器（优先消费特权异常与调度铁证）
        /// </summary>
        public ChannelReader<ApiAuditLogEntry> Reader => _reader;

        private static bool IsPrivilegedEntry(ApiAuditLogEntry entry)
        {
            // 1. 任何 4xx / 5xx 异常响应绝对属于特权铁证
            if (entry.StatusCode >= 400 || !string.IsNullOrEmpty(entry.Exception))
            {
                return true;
            }

            // 2. 调度与任务相关接口绝对属于特权铁证
            if (!string.IsNullOrEmpty(entry.Path))
            {
                if (entry.Path.Contains("/dispatch", StringComparison.OrdinalIgnoreCase) ||
                    entry.Path.Contains("/task", StringComparison.OrdinalIgnoreCase) ||
                    entry.Path.Contains("/vehicle", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // 3. 特权对接方报文
            if (string.Equals(entry.Peer, "Dispatch", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(entry.Peer, "TM", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(entry.Peer, "MES", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }
}