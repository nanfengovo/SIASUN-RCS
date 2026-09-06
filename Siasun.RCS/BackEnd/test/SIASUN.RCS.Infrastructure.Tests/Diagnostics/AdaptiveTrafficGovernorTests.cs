using System.Threading;
using Shouldly;
using SIASUN.RCS.Diagnostics;
using Xunit;

namespace SIASUN.RCS.Infrastructure.Tests.Diagnostics
{
    /// <summary>
    /// 自适应限流降采样控制器单元测试（L4 自治保护测试）
    /// </summary>
    public class AdaptiveTrafficGovernorTests
    {
        [Fact]
        public void Privileged_Events_Should_Always_Be_Admitted_Regardless_Of_Load()
        {
            var governor = new AdaptiveTrafficGovernor(elevatedThresholdEps: 10, criticalThresholdEps: 20);

            // 模拟高压注入 100 次 Warning/Error 与 Operation
            for (int i = 0; i < 100; i++)
            {
                governor.ShouldAdmit(DiagnosticCategories.Telemetry, DiagnosticLevels.Error).IsAdmitted.ShouldBeTrue();
                governor.ShouldAdmit(DiagnosticCategories.Telemetry, DiagnosticLevels.Warning).IsAdmitted.ShouldBeTrue();
                governor.ShouldAdmit(DiagnosticCategories.Operation, DiagnosticLevels.Information).IsAdmitted.ShouldBeTrue();
                governor.ShouldAdmit(DiagnosticCategories.Dispatch, DiagnosticLevels.Debug).IsAdmitted.ShouldBeTrue();
            }

            var metrics = governor.GetMetrics();
            metrics.TotalDroppedCount.ShouldBe(0);
            metrics.TotalAdmittedCount.ShouldBe(400);
        }

        [Fact]
        public void Normal_Load_Should_Admit_All_Events()
        {
            var governor = new AdaptiveTrafficGovernor(elevatedThresholdEps: 100, criticalThresholdEps: 500);

            for (int i = 0; i < 20; i++)
            {
                var decision = governor.ShouldAdmit(DiagnosticCategories.Telemetry, DiagnosticLevels.Information);
                decision.IsAdmitted.ShouldBeTrue();
                decision.CurrentLevel.ShouldBe(TrafficGovernorLevel.Normal);
            }

            governor.GetMetrics().TotalDroppedCount.ShouldBe(0);
        }

        [Fact]
        public void CriticalBurst_Load_Should_Sample_Telemetries_Aggressively()
        {
            // 设定极低阈值以触发 CriticalBurst
            var governor = new AdaptiveTrafficGovernor(elevatedThresholdEps: 5, criticalThresholdEps: 10);

            // 模拟短时间大量涌入 100 个常规遥测
            for (int i = 0; i < 50; i++)
            {
                governor.ShouldAdmit(DiagnosticCategories.Telemetry, DiagnosticLevels.Trace);
            }
            Thread.Sleep(1100); // 跨过 1 秒窗口使速率生效

            int droppedCount = 0;
            int admittedCount = 0;

            for (int i = 0; i < 50; i++)
            {
                var d = governor.ShouldAdmit(DiagnosticCategories.Telemetry, DiagnosticLevels.Trace);
                if (d.IsAdmitted) admittedCount++;
                else droppedCount++;
            }

            // 在突发洪峰下，应发生降采样丢弃
            droppedCount.ShouldBeGreaterThan(0);
        }
    }
}
