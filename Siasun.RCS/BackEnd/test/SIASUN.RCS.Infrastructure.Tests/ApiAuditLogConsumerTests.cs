using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using SIASUN.RCS.Auditing;
using SIASUN.RCS.Infrastructure.Logging;

namespace SIASUN.RCS.Infrastructure.Tests;

public class ApiAuditLogConsumerTests
{
    private readonly ApiAuditLogChannel _channel = new();
    private readonly IApiAuditLogStore _store = Substitute.For<IApiAuditLogStore>();
    private readonly ILogger<ApiAuditLogConsumer> _logger = Substitute.For<ILogger<ApiAuditLogConsumer>>();

    /// <summary>
    /// 【用例 1：正常批量消费】Channel 中有日志时，Worker 应该读取并调用 IApiAuditLogStore.SaveBatchAsync 批量保存
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenChannelHasItems_ShouldBatchSaveToStore()
    {
        // ----------------- 1. Arrange (准备测试数据) -----------------
        var capturedEntries = new List<ApiAuditLogEntry>();
        _ = _store.SaveBatchAsync(
            Arg.Do<IReadOnlyList<ApiAuditLogEntry>>(list => capturedEntries.AddRange(list)),
            Arg.Any<CancellationToken>()
        );

        _channel.TryWrite(new ApiAuditLogEntry { Path = "/api/v1/task/1", StatusCode = 200 });
        _channel.TryWrite(new ApiAuditLogEntry { Path = "/api/v1/task/2", StatusCode = 200 });
        _channel.TryWrite(new ApiAuditLogEntry { Path = "/api/v1/task/3", StatusCode = 200 });

        var consumer = new ApiAuditLogConsumer(_channel, _store, _logger);
        using var cts = new CancellationTokenSource();

        // ----------------- 2. Act (启动后台消费任务) -----------------
        var consumerTask = consumer.StartAsync(cts.Token);
        await Task.Delay(200); // 留出足够时间让后台消费循环执行
        cts.Cancel();          // 发出停止信号
        await consumer.StopAsync(CancellationToken.None);

        // ----------------- 3. Assert (断言验证结果) -----------------
        // 验证 Store 接收到了 3 条数据
        capturedEntries.Count.ShouldBe(3);
        capturedEntries.ShouldContain(x => x.Path == "/api/v1/task/1");
        capturedEntries.ShouldContain(x => x.Path == "/api/v1/task/2");
        capturedEntries.ShouldContain(x => x.Path == "/api/v1/task/3");
    }

    /// <summary>
    /// 【用例 2：大批量分批切分】当 Channel 中积压超过 50 条（例如 75 条）时，Worker 应该自动分两批（50 + 25）提交
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenMoreThan50Items_ShouldChunkIntoMultipleBatches()
    {
        // ----------------- 1. Arrange: 往 Channel 写入 75 条数据 -----------------
        for (int i = 0; i < 75; i++)
        {
            _channel.TryWrite(new ApiAuditLogEntry { Path = $"/api/bulk/{i}", StatusCode = 200 });
        }

        var consumer = new ApiAuditLogConsumer(_channel, _store, _logger);
        using var cts = new CancellationTokenSource();

        // ----------------- 2. Act -----------------
        var consumerTask = consumer.StartAsync(cts.Token);
        await Task.Delay(300); // 留出时间让 Worker 完成两轮批量写入
        cts.Cancel();
        await consumer.StopAsync(CancellationToken.None);

        // ----------------- 3. Assert: 验证被分成了 2 批提交 -----------------
        await _store.Received(2).SaveBatchAsync(
            Arg.Any<IReadOnlyList<ApiAuditLogEntry>>(),
            Arg.Any<CancellationToken>()
        );
    }

    /// <summary>
    /// 【用例 3：存储异常容错】当底层 Store 抛出数据库异常时，Worker 必须捕获异常记录日志，绝不能崩溃退出
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenStoreThrowsException_ShouldLogErrorAndKeepRunning()
    {
        // ----------------- 1. Arrange: 模拟底层数据库暂时锁死抛异常 -----------------
        _store.SaveBatchAsync(Arg.Any<IReadOnlyList<ApiAuditLogEntry>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("SQLite Database Locked"));

        _channel.TryWrite(new ApiAuditLogEntry { Path = "/api/error/test" });

        var consumer = new ApiAuditLogConsumer(_channel, _store, _logger);
        using var cts = new CancellationTokenSource();

        // ----------------- 2. Act: 运行 Worker -----------------
        var consumerTask = consumer.StartAsync(cts.Token);
        await Task.Delay(200);
        cts.Cancel();
        await consumer.StopAsync(CancellationToken.None);

        // ----------------- 3. Assert: 验证即使发生异常，Worker 也能正常处理并优雅停机 -----------------
        consumerTask.IsCompletedSuccessfully.ShouldBeTrue();
    }

    /// <summary>
    /// 【用例 4：优雅停机】当收到 CancellationToken 取消信号时，Worker 应该安全退出循环
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenCancelledImmediately_ShouldExitGracefully()
    {
        // 1. Arrange: 创建一个立即触发取消的 Token
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var consumer = new ApiAuditLogConsumer(_channel, _store, _logger);

        // 2. Act: 传入已取消的 token 执行
        await consumer.StartAsync(cts.Token);

        // 3. Assert: 任务应立即安全完成
        _channel.Reader.TryRead(out _).ShouldBeFalse();
    }
}

