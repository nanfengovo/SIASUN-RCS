using System;
using System.IO;
using Shouldly;
using SIASUN.RCS.Infrastructure.Logging.Channels;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Tests.Diagnostics
{
    /// <summary>
    /// 工业级特权铁证紧急溢出保全环单元测试
    /// 验证内存消费队列、应急本地磁盘保全与零丢失语义
    /// </summary>
    public class EvidenceSpillBufferTests : IDisposable
    {
        private readonly string _tempSpillDir;

        public EvidenceSpillBufferTests()
        {
            _tempSpillDir = Path.Combine(Path.GetTempPath(), "rcs_test_spill_" + Guid.NewGuid().ToString("N"));
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempSpillDir))
                {
                    Directory.Delete(_tempSpillDir, true);
                }
            }
            catch
            {
                // ignore
            }
        }

        [Fact]
        public void Enqueue_ShouldIncreaseSpillCountAndPersistToDisk()
        {
            // Arrange
            var buffer = new EvidenceSpillBuffer<string>("test_buffer", _tempSpillDir);

            // Act
            buffer.Enqueue("PrivilegedEvent-1");
            buffer.Enqueue("PrivilegedEvent-2");

            // Assert
            buffer.TotalSpillCount.ShouldBe(2);
            buffer.PendingSpillCount.ShouldBe(2);
            buffer.HasPendingSpills.ShouldBeTrue();

            // Verify disk persistence
            Directory.Exists(_tempSpillDir).ShouldBeTrue();
            var files = Directory.GetFiles(_tempSpillDir, "*.jsonl");
            files.Length.ShouldBe(1);

            var diskContent = File.ReadAllText(files[0]);
            diskContent.ShouldContain("PrivilegedEvent-1");
            diskContent.ShouldContain("PrivilegedEvent-2");
        }

        [Fact]
        public void TryDequeue_ShouldDrainInMemoryQueueInFifoOrder()
        {
            // Arrange
            var buffer = new EvidenceSpillBuffer<string>("test_fifo", _tempSpillDir);
            buffer.Enqueue("ItemA");
            buffer.Enqueue("ItemB");

            // Act & Assert
            buffer.TryDequeue(out var first).ShouldBeTrue();
            first.ShouldBe("ItemA");
            buffer.PendingSpillCount.ShouldBe(1);

            buffer.TryDequeue(out var second).ShouldBeTrue();
            second.ShouldBe("ItemB");
            buffer.PendingSpillCount.ShouldBe(0);
            buffer.HasPendingSpills.ShouldBeFalse();

            // Total spill count still records historical cumulative count
            buffer.TotalSpillCount.ShouldBe(2);

            // Empty queue returns false
            buffer.TryDequeue(out var none).ShouldBeFalse();
            none.ShouldBeNull();
        }

        [Fact]
        public async Task WaitForItemAsync_WhenItemEnqueued_ShouldWakeUpImmediately()
        {
            // Arrange
            var buffer = new EvidenceSpillBuffer<string>("test_wait", _tempSpillDir);

            // Act: 等待新元素，此时队列为空，Wait 应当阻塞
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var waitTask = buffer.WaitForItemAsync(cts.Token);
            waitTask.IsCompleted.ShouldBeFalse();

            // 写入元素，应立即唤醒信号量
            buffer.Enqueue("AwakeningItem");
            await waitTask;

            // Assert
            waitTask.IsCompletedSuccessfully.ShouldBeTrue();
            buffer.TryDequeue(out var item).ShouldBeTrue();
            item.ShouldBe("AwakeningItem");
        }

        [Fact]
        public void RecoverDiskSpills_ShouldIncreaseTotalRecoveredCount_WithoutInflatingTotalSpillCount()
        {
            // Arrange
            Directory.CreateDirectory(_tempSpillDir);
            var filePath = Path.Combine(_tempSpillDir, "test_recover_spill_20260906.jsonl");
            File.WriteAllLines(filePath, new[]
            {
                "\"HistoricalRecord-1\"",
                "\"HistoricalRecord-2\""
            });

            var buffer = new EvidenceSpillBuffer<string>("test_recover", _tempSpillDir);

            // Act: 从磁盘恢复
            var recoveredCount = buffer.RecoverDiskSpills();

            // Assert
            recoveredCount.ShouldBe(2);
            buffer.TotalRecoveredCount.ShouldBe(2);
            // 关键验证：从磁盘恢复的记录严禁重复累加到 TotalSpillCount，避免虚高
            buffer.TotalSpillCount.ShouldBe(0);
            buffer.PendingSpillCount.ShouldBe(2);

            buffer.TryDequeue(out var first).ShouldBeTrue();
            first.ShouldBe("HistoricalRecord-1");
            buffer.TryDequeue(out var second).ShouldBeTrue();
            second.ShouldBe("HistoricalRecord-2");
        }

        [Fact]
        public void Enqueue_WhenDiskWriteFails_ShouldIncrementSpillDiskWriteFailures()
        {
            // Arrange: 创建一个同名的文件占用目录路径，使 AppendAllText/CreateDirectory 必定抛出 IOException
            var invalidDirAsFile = Path.Combine(Path.GetTempPath(), "rcs_test_blocked_file_" + Guid.NewGuid().ToString("N"));
            File.WriteAllText(invalidDirAsFile, "blocking file");

            try
            {
                var buffer = new EvidenceSpillBuffer<string>("test_fail", invalidDirAsFile);

                // Act
                buffer.Enqueue("ItemCausingDiskFail");

                // Assert: 内存入队成功，但磁盘写入失败指标必须精确自增
                buffer.PendingSpillCount.ShouldBe(1);
                buffer.SpillDiskWriteFailures.ShouldBe(1);
            }
            finally
            {
                try { File.Delete(invalidDirAsFile); } catch { }
            }
        }
    }
}
