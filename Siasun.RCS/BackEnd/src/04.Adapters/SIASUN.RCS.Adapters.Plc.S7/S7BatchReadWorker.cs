using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SIASUN.RCS.Adapters.Plc.S7
{
    /// <summary>
    /// 西门子 S7 批量读取后台工作线程（以 100ms~500ms 周期批量轮询指定 DB 块并更新高速标签缓存）
    /// </summary>
    public class S7BatchReadWorker : BackgroundService
    {
        private readonly S7TagCache _tagCache;
        private readonly ILogger<S7BatchReadWorker> _logger;

        public S7BatchReadWorker(
            S7TagCache tagCache,
            ILogger<S7BatchReadWorker> logger)
        {
            _tagCache = tagCache;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("S7BatchReadWorker 启动，西门子 S7 批量数据块读取守护就绪");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 模拟从 PLC 读取 8 个标准槽位的 46B 遥测字节
                    var slots = new List<S7SlotData>();
                    for (var i = 1; i <= 8; i++)
                    {
                        var isOccupied = i % 2 == 0;
                        slots.Add(new S7SlotData(
                            SlotIndex: i,
                            Offset: (i - 1) * 46,
                            StatusCode: (ushort)(isOccupied ? 1 : 0),
                            CarrierCode: isOccupied ? $"FOUP_LOT_{i:D4}" : string.Empty,
                            IsPresent: isOccupied,
                            IsTilted: false,
                            LeftSensor: isOccupied,
                            RightSensor: isOccupied,
                            PlcTimestamp: (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
                    }

                    _tagCache.BatchUpdateSlots(slots);
                    _tagCache.SetTag("PLC_COMM_HEARTBEAT", DateTime.UtcNow.Ticks, true);

                    await Task.Delay(500, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "S7 批量读取轮询周期出现异常");
                    await Task.Delay(1000, stoppingToken);
                }
            }

            _logger.LogInformation("S7BatchReadWorker 已停止");
        }
    }
}
