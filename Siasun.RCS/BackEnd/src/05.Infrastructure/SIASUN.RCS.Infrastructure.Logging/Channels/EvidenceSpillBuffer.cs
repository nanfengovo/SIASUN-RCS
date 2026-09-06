using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace SIASUN.RCS.Infrastructure.Logging.Channels
{
    /// <summary>
    /// 工业级特权铁证紧急溢出保全环（Spill Buffer）
    /// 当特权通道遭遇极端洪峰或消费端短暂拥堵时，作为最后一道防线提供零丢失吸纳与本地磁盘应急落盘，
    /// 坚决杜绝工控事故关键证据（4xx/5xx 异常、调度操作、车辆任务变更）被静默丢弃
    /// </summary>
    /// <typeparam name="T">证据实体类型</typeparam>
    public class EvidenceSpillBuffer<T>
    {
        private readonly ConcurrentQueue<T> _inMemoryQueue = new();
        private readonly string _bufferName;
        private readonly string _spillDir;
        private long _totalSpillCount;
        private readonly object _diskLock = new();

        /// <summary>
        /// 累计溢出保全事件总数（系统自治观测核心指标，大于 0 即意味着发生过通道饱和与紧急溢出）
        /// </summary>
        public long TotalSpillCount => Interlocked.Read(ref _totalSpillCount);

        /// <summary>
        /// 当前待消费的紧急溢出事件数
        /// </summary>
        public int PendingSpillCount => _inMemoryQueue.Count;

        /// <summary>
        /// 是否存在待消费的溢出事件
        /// </summary>
        public bool HasPendingSpills => !_inMemoryQueue.IsEmpty;

        /// <summary>
        /// 初始化特权铁证紧急溢出保全环
        /// </summary>
        /// <param name="bufferName">缓冲区名称（如 api_audit / entity_audit）</param>
        /// <param name="spillDir">磁盘应急落盘目录（可选，默认 App_Data/spill）</param>
        public EvidenceSpillBuffer(string bufferName, string? spillDir = null)
        {
            _bufferName = bufferName;
            _spillDir = spillDir ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "spill");
        }

        /// <summary>
        /// 将一条在规定超时内未入队的特权铁证紧急写入溢出环并同步落盘保全
        /// </summary>
        /// <param name="item">特权证据实体</param>
        public void Enqueue(T item)
        {
            if (item == null) return;

            // 1. 压入内存紧急消费队列，等待后台消费者优先捞取
            _inMemoryQueue.Enqueue(item);
            Interlocked.Increment(ref _totalSpillCount);

            // 2. 应急本地磁盘保全（防止极端断电/崩溃导致内存中未消费的 Spill 丢失）
            try
            {
                if (!Directory.Exists(_spillDir))
                {
                    Directory.CreateDirectory(_spillDir);
                }

                var today = DateTime.UtcNow.ToString("yyyyMMdd");
                var filePath = Path.Combine(_spillDir, $"{_bufferName}_spill_{today}.jsonl");

                var line = JsonSerializer.Serialize(item) + Environment.NewLine;
                lock (_diskLock)
                {
                    File.AppendAllText(filePath, line);
                }
            }
            catch
            {
                // 磁盘 I/O 异常时不阻断内存队列正常运转
            }
        }

        /// <summary>
        /// 尝试从溢出环中拉取一条待消费的特权铁证
        /// </summary>
        /// <param name="item">取出的证据实体</param>
        /// <returns>是否存在待消费的溢出实体</returns>
        public bool TryDequeue(out T item)
        {
            return _inMemoryQueue.TryDequeue(out item!);
        }
    }
}
