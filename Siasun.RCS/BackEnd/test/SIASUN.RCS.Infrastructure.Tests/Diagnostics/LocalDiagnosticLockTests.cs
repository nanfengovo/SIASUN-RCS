using System;
using System.Threading.Tasks;
using Shouldly;
using SIASUN.RCS.Diagnostics;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Tests.Diagnostics
{
    /// <summary>
    /// 诊断锁单元测试（L4 多实例/集群一致性 Seam 测试）
    /// </summary>
    public class LocalDiagnosticLockTests
    {
        [Fact]
        public async Task Should_Acquire_And_Release_Lock_Properly()
        {
            var diagnosticLock = new LocalDiagnosticLock();
            var resource = "rcs:lock:test-disk-heal";

            // 1. 初次加锁应成功
            var lockToken1 = await diagnosticLock.TryAcquireLockAsync(resource, TimeSpan.FromSeconds(1));
            lockToken1.ShouldNotBeNull();
            (await diagnosticLock.IsLockedAsync(resource)).ShouldBeTrue();

            // 2. 互斥冲突：并发二次加锁应超时失败
            var lockToken2 = await diagnosticLock.TryAcquireLockAsync(resource, TimeSpan.Zero);
            lockToken2.ShouldBeNull();

            // 3. 释放锁
            lockToken1.Dispose();
            (await diagnosticLock.IsLockedAsync(resource)).ShouldBeFalse();

            // 4. 再次加锁应成功
            var lockToken3 = await diagnosticLock.TryAcquireLockAsync(resource, TimeSpan.FromSeconds(1));
            lockToken3.ShouldNotBeNull();
            lockToken3.Dispose();
        }
    }
}
