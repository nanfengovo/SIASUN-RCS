using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shouldly;
using SIASUN.RCS.Diagnostics.AI;
using SIASUN.RCS.Diagnostics.FlightPack;
using Xunit;

namespace SIASUN.RCS.Domain.Tests.Diagnostics
{
    /// <summary>
    /// 本地确定性规则因果推演诊断 Provider 单元测试
    /// 验证在物理断网或 AI 模块禁用时，能根据客观时序多轨事件输出精准的责任归属与排障报告
    /// </summary>
    public class RuleBasedIncidentAnalysisProviderTests
    {
        private readonly RuleBasedIncidentAnalysisProvider _provider = new();

        private readonly FlightPackMetadata _sampleMetadata = new()
        {
            Anchor = new AnchorDto { Type = "Task", Key = "T20260906-001", RelatedVehicleId = "AGV-01" },
            TimeWindow = new TimeWindowDto { QueryStartTime = DateTime.UtcNow.AddMinutes(-10), QueryEndTime = DateTime.UtcNow }
        };

        [Fact]
        public async Task AnalyzeIncidentAsync_WhenOperatorInterventionPresent_ShouldAttributeToOperator()
        {
            // Arrange
            var events = new List<FlightPackTimelineEvent>
            {
                new()
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-5),
                    Track = "Operator",
                    Level = "Warning",
                    Source = "Admin",
                    Title = "调度员强制取消任务",
                    Summary = "调度员取消任务 T20260906-001，原因: 现场阻挡"
                }
            };

            // Act
            var result = await _provider.AnalyzeIncidentAsync(_sampleMetadata, events, "基础因果叙事");

            // Assert
            result.ShouldNotBeNull();
            result.IsSuccess.ShouldBeTrue();
            result.ResponsibleParty.ShouldContain("现场误操作/人工干预");
            result.ConfidenceLevel.ShouldBe("High");
            result.RootCauseSummary.ShouldContain("调度员");
            result.RecommendedActions.ShouldNotBeEmpty();
            result.MarkdownReport.ShouldContain("【根因结论】");
        }

        [Fact]
        public async Task AnalyzeIncidentAsync_WhenApiErrorPresent_ShouldAttributeToUpstream()
        {
            // Arrange
            var events = new List<FlightPackTimelineEvent>
            {
                new()
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-4),
                    Track = "API",
                    Level = "Error",
                    Source = "MES",
                    Title = "POST /api/mes/carrier 500 InternalServerError",
                    Summary = "上游接口响应超时"
                }
            };

            // Act
            var result = await _provider.AnalyzeIncidentAsync(_sampleMetadata, events, "基础因果叙事");

            // Assert
            result.ShouldNotBeNull();
            result.IsSuccess.ShouldBeTrue();
            result.ResponsibleParty.ShouldContain("上游对接/下发异常");
            result.ConfidenceLevel.ShouldBe("High");
            result.RootCauseSummary.ShouldContain("外部接口通信故障");
        }

        [Fact]
        public async Task AnalyzeIncidentAsync_WhenFatalExceptionPresent_ShouldAttributeToHardware()
        {
            // Arrange
            var events = new List<FlightPackTimelineEvent>
            {
                new()
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-3),
                    Track = "Exception",
                    Level = "Fatal",
                    Source = "VehicleMonitor",
                    Title = "AGV-01 车体心跳超时断连",
                    Summary = "连续 5 次遥测丢失"
                }
            };

            // Act
            var result = await _provider.AnalyzeIncidentAsync(_sampleMetadata, events, "基础因果叙事");

            // Assert
            result.ShouldNotBeNull();
            result.IsSuccess.ShouldBeTrue();
            result.ResponsibleParty.ShouldContain("硬件通信/车体故障");
            result.ConfidenceLevel.ShouldBe("Medium");
            result.RootCauseSummary.ShouldContain("底盘通信超时或车载硬件上报故障");
            result.RootCauseSummary.ShouldContain("捕获底层核心异常或车辆遥测中断");
        }

        [Fact]
        public async Task AnalyzeIncidentAsync_WhenNoExplicitFailure_ShouldAttributeToAlgorithmOrResourceLock()
        {
            // Arrange: only normal telemetry
            var events = new List<FlightPackTimelineEvent>
            {
                new()
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-2),
                    Track = "Telemetry",
                    Level = "Information",
                    Source = "Chassis",
                    Title = "AGV-01 坐标点更新",
                    Summary = "X=100, Y=200"
                }
            };

            // Act
            var result = await _provider.AnalyzeIncidentAsync(_sampleMetadata, events, "基础因果叙事");

            // Assert
            result.ShouldNotBeNull();
            result.IsSuccess.ShouldBeTrue();
            result.ResponsibleParty.ShouldContain("调度算法/路径死锁");
            result.ConfidenceLevel.ShouldBe("Medium");
            result.RootCauseSummary.ShouldContain("未捕获致命硬件故障");
        }
    }
}
