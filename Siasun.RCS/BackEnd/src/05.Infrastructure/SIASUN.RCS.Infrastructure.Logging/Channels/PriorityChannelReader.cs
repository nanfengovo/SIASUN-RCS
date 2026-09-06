using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SIASUN.RCS.Infrastructure.Logging.Channels
{
    /// <summary>
    /// 双轨优先级通道读取器
    /// 优先消费特权铁证通道中的条目，在特权通道清空后再消费常规高频通道中的条目，确保关键铁证优先落盘与处理
    /// </summary>
    /// <typeparam name="T">通道元素类型</typeparam>
    public sealed class PriorityChannelReader<T> : ChannelReader<T>
    {
        private readonly ChannelReader<T> _priorityReader;
        private readonly ChannelReader<T> _normalReader;
        private readonly EvidenceSpillBuffer<T>? _spillBuffer;

        /// <summary>
        /// 构造双轨优先级通道读取器
        /// </summary>
        /// <param name="priorityReader">高优先级特权读取器</param>
        /// <param name="normalReader">常规高频读取器</param>
        /// <param name="spillBuffer">特权铁证紧急溢出保全环（可选，优先消费）</param>
        public PriorityChannelReader(
            ChannelReader<T> priorityReader,
            ChannelReader<T> normalReader,
            EvidenceSpillBuffer<T>? spillBuffer = null)
        {
            _priorityReader = priorityReader;
            _normalReader = normalReader;
            _spillBuffer = spillBuffer;
        }

        /// <inheritdoc />
        public override bool TryRead(out T item)
        {
            if (_spillBuffer != null && _spillBuffer.TryDequeue(out item!))
            {
                return true;
            }

            if (_priorityReader.TryRead(out item!))
            {
                return true;
            }

            return _normalReader.TryRead(out item!);
        }

        /// <inheritdoc />
        public override async ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
        {
            if ((_spillBuffer != null && _spillBuffer.PendingSpillCount > 0) ||
                _priorityReader.Count > 0 ||
                _normalReader.Count > 0)
            {
                return true;
            }

            var tPriority = _priorityReader.WaitToReadAsync(cancellationToken).AsTask();
            var tNormal = _normalReader.WaitToReadAsync(cancellationToken).AsTask();

            var completed = await Task.WhenAny(tPriority, tNormal);
            return await completed;
        }

        /// <inheritdoc />
        public override Task Completion => Task.WhenAll(_priorityReader.Completion, _normalReader.Completion);

        /// <inheritdoc />
        public override bool CanCount => _priorityReader.CanCount && _normalReader.CanCount;

        /// <inheritdoc />
        public override int Count => (_spillBuffer?.PendingSpillCount ?? 0) + _priorityReader.Count + _normalReader.Count;
    }
}
