using System.Collections.Generic;
using Volo.Abp.Application.Services;

namespace SIASUN.RCS.Monitor
{
    /// <summary>
    /// 日志级别动态控制接口（监控仪表盘使用）
    /// 允许现场排障时一键将系统的日志级别从 Info 降为 Debug，抓完报文再调回，无需重启服务。
    /// </summary>
    public interface ILogControlAppService : IApplicationService
    {
        /// <summary>
        /// 获取所有支持动态调级的日志命名空间及其当前级别
        /// </summary>
        /// <returns>命名空间与日志级别的键值对 (如 "SIASUN.RCS": "Information")</returns>
        Dictionary<string, string> GetLevels();

        /// <summary>
        /// 动态调整指定命名空间的日志级别
        /// </summary>
        /// <param name="namespaceName" example="SIASUN.RCS">命名空间，留空表示全局 Default</param>
        /// <param name="level" example="Debug">目标日志级别 (Verbose, Debug, Information, Warning, Error, Fatal)</param>
        /// <returns>是否设置成功</returns>
        bool SetLevel(string namespaceName, string level);
    }
}

