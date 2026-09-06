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
        private long _spillDiskWriteFailures;
        private readonly object _diskLock = new();

        /// <summary>
        /// 累计溢出保全事件总数（系统自治观测核心指标，大于 0 即意味着发生过通道饱和与紧急溢出）
        /// </summary>
        public long TotalSpillCount => Interlocked.Read(ref _totalSpillCount);

        /// <summary>
        /// 累计应急落盘磁盘写入失败次数（大于 0 说明工控机本地磁盘写保护、爆满或 I/O 故障）
        /// </summary>
        public long SpillDiskWriteFailures => Interlocked.Read(ref _spillDiskWriteFailures);

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
        /// <param name="bufferName">缓冲区名称（如 api_audit / entity_audit / operation_log）</param>
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
            catch (Exception ex)
            {
                Interlocked.Increment(ref _spillDiskWriteFailures);
                Console.Error.WriteLine($"[CRITICAL-SPILL-DISK-FAILURE] Failed to write spill file for {_bufferName}: {ex.Message}");
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

        /// <summary>
        /// 从本地磁盘回放未消费的溢出文件至内存队列（服务启动自愈或定时容灾回放）
        /// 回放成功的持久化文件将自动归档为 .replayed 后缀，防止进程重启导致内存外证据遗失
        /// </summary>
        /// <returns>成功从磁盘回放恢复的特权证据数量</returns>
        public int RecoverDiskSpills()
        {
            if (!Directory.Exists(_spillDir))
            {
                return 0;
            }

            var recoveredCount = 0;
            lock (_diskLock)
            {
                try
                {
                    var pattern = $"{_bufferName}_spill_*.jsonl";
                    var spillFiles = Directory.GetFiles(_spillDir, pattern);

                    foreach (var file in spillFiles)
                    {
                        if (file.EndsWith(".replayed", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var lines = File.ReadAllLines(file);
                        foreach (var line in lines)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            try
                            {
                                var item = JsonSerializer.Deserialize<T>(line);
                                if (item != null)
                                {
                                    _inMemoryQueue.Enqueue(item);
                                    Interlocked.Increment(ref _totalSpillCount);
                                    recoveredCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine($"[CRITICAL-SPILL-DESERIALIZE-FAILURE] Corrupted line in {file}: {ex.Message}");
                            }
                        }

                        // 回放完毕后原子归档，杜绝重启重复入库
                        var replayedFile = file + ".replayed";
                        if (File.Exists(replayedFile))
                        {
                            File.Delete(replayedFile);
                        }
                        File.Move(file, replayedFile);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[CRITICAL-SPILL-RECOVERY-FAILURE] Failed to recover disk spills for {_bufferName}: {ex.Message}");
                }
            }

            return recoveredCount;
        }
    }
}
