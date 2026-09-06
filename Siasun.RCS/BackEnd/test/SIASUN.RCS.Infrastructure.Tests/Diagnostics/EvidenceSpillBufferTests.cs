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
    }
}
