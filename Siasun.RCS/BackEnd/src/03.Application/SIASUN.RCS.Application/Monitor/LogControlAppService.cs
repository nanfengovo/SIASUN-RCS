using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using SIASUN.RCS.Logging;
using SIASUN.RCS.Permissions;
using Volo.Abp.Application.Services;

namespace SIASUN.RCS.Monitor
{
    /// <summary>
    /// 运行时日志级别动态控制应用服务
    /// 支持生产现场在线提升或降低指定命名空间的日志输出粒度
    /// </summary>
    [Authorize(RCSPermissions.LogControl.Default)]
    public class LogControlAppService : ApplicationService, ILogControlAppService
    {
        private readonly IDynamicLogSwitchRegistry _registry;
        private readonly SIASUN.RCS.Interfaces.OperationLogs.IOperationLogRecorder _operationLogRecorder;

        /// <summary>
        /// 构造函数注入动态日志开关注册表与操作审计记录器
        /// </summary>
        /// <param name="registry">动态日志开关注册表</param>
        /// <param name="operationLogRecorder">操作审计记录器</param>
        public LogControlAppService(IDynamicLogSwitchRegistry registry, SIASUN.RCS.Interfaces.OperationLogs.IOperationLogRecorder operationLogRecorder)
        {
            _registry = registry;
            _operationLogRecorder = operationLogRecorder;
        }

        /// <summary>
        /// 获取所有受控命名空间的当前日志级别快照
        /// </summary>
        public Dictionary<string, string> GetLevels()
        {
            return _registry.GetLevels();
        }

        /// <summary>
        /// 动态调整指定命名空间的运行时日志级别
        /// </summary>
        /// <param name="namespaceName">目标命名空间</param>
        /// <param name="level">日志级别 (Trace, Debug, Information, Warning, Error, Critical, None)</param>
        /// <returns>是否设置成功</returns>
        [Authorize(RCSPermissions.LogControl.SetLevel)]
        public bool SetLevel(string namespaceName, string level)
        {
            if (System.Enum.TryParse<Microsoft.Extensions.Logging.LogLevel>(level, true, out var parsedLevel))
            {
                var oldLevel = _registry.GetLevels().TryGetValue(namespaceName, out var lvl) ? lvl : "Default";
                var success = _registry.TrySetLevel(namespaceName, parsedLevel);
                if (success)
                {
                    _operationLogRecorder.Record(new SIASUN.RCS.Logs.OperatorLogs.OperationLogContext
                    {
                        Module = "LogControl",
                        Action = "SetLevel",
                        TargetType = "Logger",
                        TargetId = namespaceName,
                        BeforeState = oldLevel,
                        AfterState = parsedLevel.ToString(),
                        Reason = $"现场运维人员动态调整日志级别 [{namespaceName}] 为 [{parsedLevel}]"
                    });
                }
                return success;
            }
            return false;
        }
    }
}
