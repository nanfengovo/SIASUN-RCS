using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using SIASUN.RCS.Hardware;
using Volo.Abp;
using Xunit;

namespace SIASUN.RCS.Domain.Tests.Hardware
{
    /// <summary>
    /// 工业硬件网关适配器注册表单元测试
    /// </summary>
    public class HardwareGateRegistryTests
    {
        /// <summary>
        /// 测试网关注册与精准解析
        /// </summary>
        [Fact]
        public void Registry_Should_Register_And_Resolve_Gates()
        {
            var airGate = Substitute.For<IHardwareGate>();
            airGate.GateType.Returns("AirShowerDoor");

            var twinGate = Substitute.For<IHardwareGate>();
            twinGate.GateType.Returns("TwinArmInterlock");

            var registry = new HardwareGateRegistry(new[] { airGate, twinGate }, NullLogger<HardwareGateRegistry>.Instance);

            registry.GetGate("AirShowerDoor").ShouldBe(airGate);
            registry.GetGate("TwinArmInterlock").ShouldBe(twinGate);

            registry.TryGetGate("AirShowerDoor", out var resolvedAir).ShouldBeTrue();
            resolvedAir.ShouldBe(airGate);

            registry.TryGetGate("NonExistent", out var nonExistent).ShouldBeFalse();
            nonExistent.ShouldBeNull();

            var types = registry.GetAllRegisteredGateTypes();
            types.Count.ShouldBe(2);
            types.ShouldContain("AirShowerDoor");
            types.ShouldContain("TwinArmInterlock");
        }

        /// <summary>
        /// 测试在未注册具体网关但存在 Mock 网关时，自动安全回退至 Mock 适配器
        /// </summary>
        [Fact]
        public void Registry_Should_Fallback_To_Mock_When_Gate_Not_Found()
        {
            var mockGate = Substitute.For<IHardwareGate>();
            mockGate.GateType.Returns("Mock");

            var registry = new HardwareGateRegistry(new[] { mockGate }, NullLogger<HardwareGateRegistry>.Instance);

            var resolved = registry.GetGate("UnknownPlcInterlock");
            resolved.ShouldBe(mockGate);
        }

        /// <summary>
        /// 测试在未注册具体网关且缺少 Mock 适配器时抛出 BusinessException
        /// </summary>
        [Fact]
        public void Registry_Should_Throw_When_No_Fallback_Available()
        {
            var registry = new HardwareGateRegistry(Array.Empty<IHardwareGate>(), NullLogger<HardwareGateRegistry>.Instance);

            Should.Throw<BusinessException>(() =>
            {
                registry.GetGate("UnknownPlcInterlock");
            }).Message.ShouldContain("未找到已注册的硬件适配器");
        }
    }
}
