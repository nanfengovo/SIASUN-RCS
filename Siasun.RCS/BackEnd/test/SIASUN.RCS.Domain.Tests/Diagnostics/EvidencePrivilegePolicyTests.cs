using Shouldly;
using SIASUN.RCS.Diagnostics;
using Xunit;

namespace SIASUN.RCS.Domain.Tests.Diagnostics
{
    /// <summary>
    /// 证据特权判定策略单元测试
    /// 验证特权单一定义点（Single Source of Truth）对异常、操作轨、核心实体及高危 HTTP 状态码的精准判定
    /// </summary>
    public class EvidencePrivilegePolicyTests
    {
        private readonly IEvidencePrivilegePolicy _policy = DefaultEvidencePrivilegePolicy.Instance;

        [Theory]
        [InlineData("Warning", true)]
        [InlineData("Error", true)]
        [InlineData("Fatal", true)]
        [InlineData("Critical", true)]
        [InlineData("Info", false)]
        [InlineData("Debug", false)]
        [InlineData("Trace", false)]
        [InlineData(null, false)]
        public void IsPrivilegedLevel_EvaluatesLevelCorrectly(string? level, bool expected)
        {
            _policy.IsPrivilegedLevel(level).ShouldBe(expected);
        }

        [Theory]
        [InlineData(500, true)]
        [InlineData(400, true)]
        [InlineData(404, true)]
        [InlineData(200, false)]
        [InlineData(null, false)]
        public void IsPrivilegedLevel_EvaluatesStatusCodeCorrectly(int? statusCode, bool expected)
        {
            _policy.IsPrivilegedLevel("Info", statusCode: statusCode).ShouldBe(expected);
        }

        [Fact]
        public void IsPrivilegedLevel_WhenExceptionPresent_ShouldBeTrue()
        {
            _policy.IsPrivilegedLevel("Info", statusCode: 200, exception: "System.TimeoutException").ShouldBeTrue();
        }

        [Theory]
        [InlineData("Operation", true)]
        [InlineData("Operator", true)]
        [InlineData("SelfHeal", true)]
        [InlineData("Dispatch", true)]
        [InlineData("AgvTask", true)]
        [InlineData("Task", true)]
        [InlineData("AgvVehicle", true)]
        [InlineData("Vehicle", true)]
        [InlineData("TM", true)]
        [InlineData("MES", true)]
        [InlineData("Exception", true)]
        [InlineData("Telemetry", false)]
        [InlineData("Heartbeat", false)]
        [InlineData(null, false)]
        public void IsPrivilegedCategory_EvaluatesCategoryCorrectly(string? category, bool expected)
        {
            _policy.IsPrivilegedCategory(category).ShouldBe(expected);
        }

        [Theory]
        [InlineData("/api/app/dispatch-intervention/cancel-task", true)]
        [InlineData("/api/app/agv-task/create", true)]
        [InlineData("/api/app/agv-vehicle/status", true)]
        [InlineData("/api/app/system-monitor/system-resources", false)]
        [InlineData(null, false)]
        public void IsPrivilegedPath_EvaluatesPathCorrectly(string? path, bool expected)
        {
            _policy.IsPrivilegedPath(path).ShouldBe(expected);
        }

        [Fact]
        public void IsPrivileged_WhenAnyDimensionPrivileged_ShouldBeTrue()
        {
            // Level is Info, Category is Telemetry, but path is /task -> should be true
            _policy.IsPrivileged(category: "Telemetry", level: "Info", path: "/api/app/task/1").ShouldBeTrue();

            // All normal -> should be false
            _policy.IsPrivileged(category: "Telemetry", level: "Info", path: "/ping").ShouldBeFalse();
        }
    }
}
