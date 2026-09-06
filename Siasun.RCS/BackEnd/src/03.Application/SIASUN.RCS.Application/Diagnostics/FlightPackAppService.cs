using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SIASUN.RCS.Diagnostics.FlightPack;
using SIASUN.RCS.Logs.OperatorLogs;
using SIASUN.RCS.Permissions;

namespace SIASUN.RCS.Diagnostics
{
    /// <summary>
    /// 黑匣子全时序事故取证排障应用服务
    /// 承载现场事故黑匣子导出、安全鉴权与排障包生成
    /// </summary>
    [Authorize(RCSPermissions.FlightPack.Default)]
    public class FlightPackAppService : RCSAppService, IFlightPackAppService
    {
        private readonly IFlightPackCollector _flightPackCollector;

        /// <summary>
        /// 构造函数注入黑匣子收集器
        /// </summary>
        /// <param name="flightPackCollector">黑匣子数据收集器</param>
        public FlightPackAppService(IFlightPackCollector flightPackCollector)
        {
            _flightPackCollector = flightPackCollector;
        }

        /// <summary>
        /// 导出事故排障黑匣子数据包 (.rcspack)
        /// </summary>
        /// <param name="input">排障导出请求参数</param>
        /// <returns>ZIP 二进制字节流</returns>
        [Authorize(RCSPermissions.FlightPack.Export)]
        [OperationLog(Module = "Diagnostics", Action = "ExportFlightPack", TargetType = "FlightPack", Description = "导出事故排障黑匣子取证包")]
        public async Task<byte[]> ExportAsync(ExportFlightPackDto input)
        {
            var request = new FlightPackRequest
            {
                AnchorType = input.AnchorType,
                AnchorKey = input.AnchorKey,
                StartTime = input.StartTime,
                EndTime = input.EndTime,
                BufferBeforeMinutes = input.BufferBeforeMinutes > 0 ? input.BufferBeforeMinutes : 5,
                BufferAfterMinutes = input.BufferAfterMinutes > 0 ? input.BufferAfterMinutes : 5,
                ExportedByUserId = CurrentUser.Id,
                ExportedByUserName = CurrentUser.UserName ?? "System",
                ClientIp = string.Empty,
                EnableAiAnalysis = input.EnableAiAnalysis
            };

            return await _flightPackCollector.CollectAndPackAsync(request);
        }
    }
}
