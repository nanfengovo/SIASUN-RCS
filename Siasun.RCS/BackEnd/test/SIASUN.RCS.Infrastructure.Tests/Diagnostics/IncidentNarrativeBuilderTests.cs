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
    }
}
