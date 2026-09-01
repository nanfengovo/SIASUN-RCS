using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace SIASUN.RCS.Infrastructure.Logging
{
    /// <summary>
    /// 全局动态日志级别切换注册表
    /// </summary>
    public class DynamicLogSwitchRegistry
    {
        public LoggingLevelSwitch GlobalSwitch { get; }

        public ConcurrentDictionary<string, LoggingLevelSwitch> NamespaceSwitches { get; }

        public DynamicLogSwitchRegistry()
        {
            GlobalSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
            NamespaceSwitches = new ConcurrentDictionary<string, LoggingLevelSwitch>();

            // 预设几个常见核心域的动态开关
            NamespaceSwitches.TryAdd("SIASUN", new LoggingLevelSwitch(LogEventLevel.Information));
            NamespaceSwitches.TryAdd("Microsoft", new LoggingLevelSwitch(LogEventLevel.Warning));
            NamespaceSwitches.TryAdd("System", new LoggingLevelSwitch(LogEventLevel.Warning));
            NamespaceSwitches.TryAdd("Microsoft.EntityFrameworkCore", new LoggingLevelSwitch(LogEventLevel.Warning));
        }

        public LoggingLevelSwitch GetOrAddSwitch(string namespacePrefix, LogEventLevel defaultLevel)
        {
            return NamespaceSwitches.GetOrAdd(namespacePrefix, _ => new LoggingLevelSwitch(defaultLevel));
        }
    }
}

