using System;
using System.Collections.Generic;
using Shouldly;
using SIASUN.RCS.Diagnostics.FlightPack;
using SIASUN.RCS.Infrastructure.Logging.Diagnostics;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Tests.Diagnostics
{
    public class IncidentNarrativeBuilderTests
    {
        private readonly IncidentNarrativeBuilder _builder = new();

        [Fact]
        public void BuildMarkdownNarrative_With_EmptyEvents_Should_Render_Fallback()
        {
            var metadata = new FlightPackMetadata
            {
                Anchor = new AnchorDto { Type = "Task", Key = "T-1001", RelatedVehicleId = "AGV-01" },
                TimeWindow = new TimeWindowDto { QueryStartTime = DateTime.UtcNow.AddMinutes(-10), QueryEndTime = DateTime.UtcNow }
            };

            var md = _builder.BuildMarkdownNarrative(metadata, new List<FlightPackTimelineEvent>());

            md.ShouldNotBeNullOrWhiteSpace();
            md.ShouldContain("T-1001");
            md.ShouldContain("AGV-01");
            md.ShouldContain("未检索到关联时序事件");
            md.ShouldContain("未捕获到 Warning 或 Error 级异常事件");
        }

        [Fact]
        public void BuildMarkdownNarrative_With_Anomalies_Should_Identify_FirstDomino()
        {
            var baseTime = new DateTime(2026, 9, 4, 10, 0, 0, DateTimeKind.Utc);
            var metadata = new FlightPackMetadata
            {
                Anchor = new AnchorDto { Type = "Task", Key = "T-1002", RelatedVehicleId = "AGV-02" },
                TimeWindow = new TimeWindowDto { QueryStartTime = baseTime, QueryEndTime = baseTime.AddMinutes(10) }
            };

            var events = new List<FlightPackTimelineEvent>
            {
                new()
                {
                    Timestamp = baseTime.AddSeconds(1),
                    Track = "API",
                    Level = "Information",
                    Source = "MES",
                    Title = "MES 下发任务",
                    Summary = "T-1002"
                },
                new()
                {
                    Timestamp = baseTime.AddSeconds(10),
                    Track = "API",
                    Level = "Warning",
                    Source = "TM",
                    Title = "TM 车辆避障中",
                    Summary = "Obstacle detected"
                },
                new()
                {
                    Timestamp = baseTime.AddSeconds(30),
                    Track = "Exception",
                    Level = "Error",
                    Source = "Engine",
                    Title = "任务超时",
                    Summary = "Timeout on station 1"
                }
            };

            var md = _builder.BuildMarkdownNarrative(metadata, events);

            md.ShouldContain("第一多米诺骨牌 (最早异常触发点)");
            md.ShouldContain("TM 车辆避障中");
            md.ShouldContain("大模型离线提问提示词");
        }

        [Fact]
        public void BuildMarkdownNarrative_With_OperatorIntervention_AfterAnomaly_Should_Classify_As_Maintenance()
        {
            var baseTime = new DateTime(2026, 9, 4, 10, 0, 0, DateTimeKind.Utc);
            var metadata = new FlightPackMetadata
            {
                Anchor = new AnchorDto { Type = "Task", Key = "T-1003", RelatedVehicleId = "AGV-03" },
                TimeWindow = new TimeWindowDto { QueryStartTime = baseTime, QueryEndTime = baseTime.AddMinutes(10) }
            };

            var events = new List<FlightPackTimelineEvent>
            {
                new()
                {
                    Timestamp = baseTime.AddSeconds(5),
                    Track = "API",
                    Level = "Error",
                    Source = "TM",
                    Title = "TM 车辆脱轨报警",
                    Summary = "Derailment detected"
                },
                new()
                {
                    Timestamp = baseTime.AddSeconds(15),
                    Track = "Operator",
                    Level = "Information",
                    Source = "User",
                    Title = "调度员点击【强制取消】",
                    Summary = "现场车辆脱轨需要急停"
                }
            };

            var md = _builder.BuildMarkdownNarrative(metadata, events);

            md.ShouldContain("已知故障后的运维处置行为");
        }

        [Fact]
        public void BuildMarkdownNarrative_With_OperatorIntervention_BeforeAnomaly_Should_Classify_As_PotentialCause()
        {
            var baseTime = new DateTime(2026, 9, 4, 10, 0, 0, DateTimeKind.Utc);
            var metadata = new FlightPackMetadata
            {
                Anchor = new AnchorDto { Type = "Task", Key = "T-1004", RelatedVehicleId = "AGV-04" },
                TimeWindow = new TimeWindowDto { QueryStartTime = baseTime, QueryEndTime = baseTime.AddMinutes(10) }
            };

            var events = new List<FlightPackTimelineEvent>
            {
                new()
                {
                    Timestamp = baseTime.AddSeconds(5),
                    Track = "Operator",
                    Level = "Information",
                    Source = "User",
                    Title = "调度员点击【修改地图参数】",
                    Summary = "变更路口通行权限"
                },
                new()
                {
                    Timestamp = baseTime.AddSeconds(15),
                    Track = "Exception",
                    Level = "Error",
                    Source = "Engine",
                    Title = "路径死锁检测",
                    Summary = "Deadlock on crossroad"
                }
            };

            var md = _builder.BuildMarkdownNarrative(metadata, events);

            md.ShouldContain("先于系统报警出现，需重点核实是否由于现场误操作");
        }

        [Fact]
        public void BuildMarkdownNarrative_With_ConsecutiveHeartbeats_Should_Fold_And_Denoise()
        {
            var baseTime = new DateTime(2026, 9, 4, 10, 0, 0, DateTimeKind.Utc);
            var metadata = new FlightPackMetadata
            {
                Anchor = new AnchorDto { Type = "Vehicle", Key = "AGV-01", RelatedVehicleId = "AGV-01" },
                TimeWindow = new TimeWindowDto { QueryStartTime = baseTime, QueryEndTime = baseTime.AddMinutes(10) }
            };

            var events = new List<FlightPackTimelineEvent>();
            // 模拟连续 50 次心跳事件
            for (int i = 0; i < 50; i++)
            {
                events.Add(new FlightPackTimelineEvent
                {
                    Timestamp = baseTime.AddSeconds(i),
                    Track = "Telemetry",
                    Level = "Information",
                    Source = "AGV-01",
                    Title = "Vehicle Heartbeat",
                    Summary = "Battery: 95%"
                });
            }

            var md = _builder.BuildMarkdownNarrative(metadata, events);

            // 验证心跳被成功合并折叠，而不是打印 50 行
            md.ShouldContain("连续 50 次采样，已自动折叠降噪");
            md.ShouldContain("Vehicle Heartbeat");
        }

        [Fact]
        public void BuildMarkdownNarrative_With_ExcessiveEvents_Should_Truncate_With_Notice()
        {
            var baseTime = new DateTime(2026, 9, 4, 10, 0, 0, DateTimeKind.Utc);
            var metadata = new FlightPackMetadata
            {
                Anchor = new AnchorDto { Type = "Task", Key = "T-1005", RelatedVehicleId = "AGV-05" },
                TimeWindow = new TimeWindowDto { QueryStartTime = baseTime, QueryEndTime = baseTime.AddMinutes(10) }
            };

            var events = new List<FlightPackTimelineEvent>();
            // 构造 250 个非心跳事件
            for (int i = 0; i < 250; i++)
            {
                events.Add(new FlightPackTimelineEvent
                {
                    Timestamp = baseTime.AddSeconds(i),
                    Track = "API",
                    Level = "Information",
                    Source = $"Service_{i}",
                    Title = $"Step Action #{i}",
                    Summary = $"Payload {i}"
                });
            }

            var md = _builder.BuildMarkdownNarrative(metadata, events);

            // 验证截断提示
            md.ShouldContain("已自动省略中间 50 条常规事件");
        }

        [Fact]
        public void BuildMarkdownNarrative_With_SensorFlapping_Should_Fold_Into_Flicker_Warning()
        {
            var baseTime = new DateTime(2026, 9, 4, 10, 0, 0, DateTimeKind.Utc);
            var metadata = new FlightPackMetadata
            {
                Anchor = new AnchorDto { Type = "Task", Key = "T-1006", RelatedVehicleId = "AGV-06" },
                TimeWindow = new TimeWindowDto { QueryStartTime = baseTime, QueryEndTime = baseTime.AddMinutes(10) }
            };

            var events = new List<FlightPackTimelineEvent>();
            // 构造 6 次高频震荡闪烁事件
            for (int i = 0; i < 6; i++)
            {
                events.Add(new FlightPackTimelineEvent
                {
                    Timestamp = baseTime.AddSeconds(i * 2),
                    Track = "Hardware",
                    Level = i % 2 == 0 ? "Warning" : "Information",
                    Source = "Sensor-Dock-01",
                    Title = i % 2 == 0 ? "光电开关遮挡" : "光电开关清除",
                    Summary = $"Signal bounce state {i}"
                });
            }

            var md = _builder.BuildMarkdownNarrative(metadata, events);

            // 验证高频抖动折叠
            md.ShouldContain("Sensor-Dock-01 状态频繁抖动/信号震荡");
            md.ShouldContain("已智能降维折叠");
            md.ShouldContain("6 次跳变");
        }
    }
}
