using System.Threading.Tasks;
using SIASUN.RCS.Diagnostics.FlightPack;

namespace SIASUN.RCS.Diagnostics
{
    /// <summary>
    /// 黑匣子全时序事故取证排障应用服务
    /// </summary>
    public class FlightPackAppService : RCSAppService, IFlightPackAppService
    {
        private readonly IFlightPackCollector _flightPackCollector;

        public FlightPackAppService(IFlightPackCollector flightPackCollector)
        {
            _flightPackCollector = flightPackCollector;
        }

        /// <summary>
        /// 导出事故排障黑匣子数据包 (.rcspack)
        /// </summary>
        /// <param name="input">排障导出请求参数</param>
        /// <returns>ZIP 二进制字节流</returns>
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
