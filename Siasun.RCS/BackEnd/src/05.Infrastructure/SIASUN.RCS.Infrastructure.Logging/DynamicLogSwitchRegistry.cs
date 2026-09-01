using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using SIASUN.RCS.Logging;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Infrastructure.Logging
{
    /// <summary>
    /// 全局动态日志级别切换注册表
    /// </summary>
    public class DynamicLogSwitchRegistry : IDynamicLogSwitchRegistry, ISingletonDependency
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

        public Dictionary<string, string> GetLevels()
        {
            var dict = new Dictionary<string, string>
            {
                { "Global", GlobalSwitch.MinimumLevel.ToString() }
            };
            foreach (var kvp in NamespaceSwitches)
            {
                dict[kvp.Key] = kvp.Value.MinimumLevel.ToString();
            }
            return dict;
        }

        public bool TrySetLevel(string namespacePrefix, LogLevel level)
        {
            LogEventLevel serilogLevel = level switch
            {
                LogLevel.Trace => LogEventLevel.Verbose,
                LogLevel.Debug => LogEventLevel.Debug,
                LogLevel.Information => LogEventLevel.Information,
                LogLevel.Warning => LogEventLevel.Warning,
                LogLevel.Error => LogEventLevel.Error,
                LogLevel.Critical => LogEventLevel.Fatal,
                LogLevel.None => LogEventLevel.Fatal,
                _ => LogEventLevel.Information
            };

            if (string.IsNullOrEmpty(namespacePrefix) || namespacePrefix == "Global")
            {
                GlobalSwitch.MinimumLevel = serilogLevel;
                return true;
            }

            if (NamespaceSwitches.TryGetValue(namespacePrefix, out var sw))
            {
                sw.MinimumLevel = serilogLevel;
                return true;
            }

            // 如果原来没有，则加进去
            NamespaceSwitches.TryAdd(namespacePrefix, new LoggingLevelSwitch(serilogLevel));
            return true;
        }
    }
}
