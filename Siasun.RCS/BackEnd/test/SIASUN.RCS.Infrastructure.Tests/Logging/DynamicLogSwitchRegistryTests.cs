using Microsoft.Extensions.Logging;
using Serilog.Events;
using Shouldly;
using SIASUN.RCS.Infrastructure.Logging;

namespace SIASUN.RCS.Infrastructure.Tests.Logging
{
    public class DynamicLogSwitchRegistryTests
    {
        [Fact]
        public void Constructor_Should_Initialize_Default_Switches()
        {
            var registry = new DynamicLogSwitchRegistry();

            registry.GlobalSwitch.ShouldNotBeNull();
            registry.GlobalSwitch.MinimumLevel.ShouldBe(LogEventLevel.Information);

            registry.NamespaceSwitches.ShouldContainKey("SIASUN");
            registry.NamespaceSwitches["SIASUN"].MinimumLevel.ShouldBe(LogEventLevel.Information);

            registry.NamespaceSwitches.ShouldContainKey("Microsoft");
            registry.NamespaceSwitches["Microsoft"].MinimumLevel.ShouldBe(LogEventLevel.Warning);
        }

        [Fact]
        public void GetOrAddSwitch_Should_Return_Existing_Or_Add_New()
        {
            var registry = new DynamicLogSwitchRegistry();

            var existingSwitch = registry.GetOrAddSwitch("SIASUN", LogEventLevel.Error);
            existingSwitch.MinimumLevel.ShouldBe(LogEventLevel.Information); // Should keep existing

            var newSwitch = registry.GetOrAddSwitch("Custom.Namespace", LogEventLevel.Debug);
            newSwitch.MinimumLevel.ShouldBe(LogEventLevel.Debug);
            registry.NamespaceSwitches.ShouldContainKey("Custom.Namespace");
        }

        [Fact]
        public void GetLevels_Should_Return_All_Configured_Levels()
        {
            var registry = new DynamicLogSwitchRegistry();

            var levels = registry.GetLevels();

            levels.ShouldContainKey("Global");
            levels["Global"].ShouldBe("Information");
            levels.ShouldContainKey("SIASUN");
            levels["SIASUN"].ShouldBe("Information");
        }

        [Theory]
        [InlineData("Global", LogLevel.Trace, LogEventLevel.Verbose)]
        [InlineData("", LogLevel.Debug, LogEventLevel.Debug)]
        [InlineData("SIASUN", LogLevel.Warning, LogEventLevel.Warning)]
        [InlineData("SIASUN", LogLevel.Error, LogEventLevel.Error)]
        [InlineData("SIASUN", LogLevel.Critical, LogEventLevel.Fatal)]
        [InlineData("SIASUN", LogLevel.None, LogEventLevel.Fatal)]
        [InlineData("New.Prefix", LogLevel.Information, LogEventLevel.Information)]
        public void TrySetLevel_Should_Update_Correct_Switch(string prefix, LogLevel level, LogEventLevel expectedSerilogLevel)
        {
            var registry = new DynamicLogSwitchRegistry();

            var result = registry.TrySetLevel(prefix, level);

            result.ShouldBeTrue();
            if (string.IsNullOrEmpty(prefix) || prefix == "Global")
            {
                registry.GlobalSwitch.MinimumLevel.ShouldBe(expectedSerilogLevel);
            }
            else
            {
                registry.NamespaceSwitches[prefix].MinimumLevel.ShouldBe(expectedSerilogLevel);
            }
        }
    }
}

