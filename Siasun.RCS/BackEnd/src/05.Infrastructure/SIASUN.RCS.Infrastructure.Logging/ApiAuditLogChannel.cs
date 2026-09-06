using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Diagnostics;
using SIASUN.RCS.Infrastructure.Logging.Channels;

namespace SIASUN.RCS.Infrastructure.Logging
{
    /// <summary>
    /// API 接口报文审计异步管道管理器
    /// 采用双轨有界队列设计：特权异常与调度报文走不可丢弃通道（Wait 模式 + Spill 溢出保全），常规心跳与成功报文走保盘降采样通道（DropOldest 模式）
    /// </summary>
    public class ApiAuditLogChannel : SIASUN.RCS.Auditing.IApiAuditLogChannel
    {
        private readonly Channel<ApiAuditLogEntry> _priorityChannel;
        private readonly Channel<ApiAuditLogEntry> _normalChannel;
        private readonly PriorityChannelReader<ApiAuditLogEntry> _reader;
        private readonly EvidenceSpillBuffer<ApiAuditLogEntry> _spillBuffer;
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
        /// 特权通道当前堆积队列深度
        /// </summary>
        public int PriorityQueueCount => _priorityChannel.Reader.Count;

        /// <summary>
        /// 常规通道当前堆积队列深度
        /// </summary>
        public int NormalQueueCount => _normalChannel.Reader.Count;

        /// <summary>
        /// 综合通道总等待深度（含溢出环待消费数）
        /// </summary>
        public int TotalQueueCount => _reader.Count;

        /// <summary>
        /// 默认构造函数，初始化双轨 API 报文审计通道与特权溢出保全环
        /// </summary>
        /// <param name="privilegePolicy">特权证据策略（可选，默认为 DefaultEvidencePrivilegePolicy）</param>
        /// <param name="spillDir">紧急溢出落盘目录（可选）</param>
        public ApiAuditLogChannel(IEvidencePrivilegePolicy? privilegePolicy = null, string? spillDir = null)
        {
            _privilegePolicy = privilegePolicy ?? DefaultEvidencePrivilegePolicy.Instance;
            _spillBuffer = new EvidenceSpillBuffer<ApiAuditLogEntry>("api_audit", spillDir);

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

            _reader = new PriorityChannelReader<ApiAuditLogEntry>(_priorityChannel.Reader, _normalChannel.Reader, _spillBuffer);
        }

        /// <summary>
        /// 尝试向通道写入一条 API 报文条目，根据优先级自动路由至特权或常规通道
        /// 针对特权异常与调度接口铁证，排队满载时自动短暂等待槽位；若依然拥堵，无缝推入溢出环紧急落盘保全，坚决杜绝静默丢弃
        /// </summary>
        /// <param name="entry">报文审计条目</param>
        /// <returns>是否成功入队或保全</returns>
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
                // 为达成 L4 铁证零丢失，特权条目进行短暂阻塞写入等待（至多 1 秒）
                try
                {
                    var writeTask = _priorityChannel.Writer.WriteAsync(entry).AsTask();
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
                _spillBuffer.Enqueue(entry);
                return true;
            }

            return _normalChannel.Writer.TryWrite(entry);
        }

        /// <summary>
        /// 异步向通道写入一条 API 报文条目，特权异常条目在缓冲区满时将异步等待槽位；若发生意外取消，自动推入溢出环紧急保全
        /// </summary>
        /// <param name="entry">报文审计条目</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async ValueTask WriteAsync(ApiAuditLogEntry entry, CancellationToken cancellationToken = default)
        {
            if (entry == null) return;

            if (IsPrivilegedEntry(entry))
            {
                try
                {
                    await _priorityChannel.Writer.WriteAsync(entry, cancellationToken);
                }
                catch
                {
                    // 异步被取消或异常时，执行紧急溢出保全
                    _spillBuffer.Enqueue(entry);
                }
                return;
            }

            _normalChannel.Writer.TryWrite(entry);
        }

        /// <summary>
        /// 获取双轨优先读取器（优先消费溢出环与特权异常调度铁证）
        /// </summary>
        public ChannelReader<ApiAuditLogEntry> Reader => _reader;

        private bool IsPrivilegedEntry(ApiAuditLogEntry entry)
        {
            return _privilegePolicy.IsPrivileged(
                path: entry.Path,
                peer: entry.Peer,
                statusCode: entry.StatusCode,
                exception: entry.Exception);
        }
    }
}
