using System.Threading;
using System.Threading.Tasks;

namespace SIASUN.RCS.Diagnostics.FlightPack
{
    /// <summary>
    /// 黑匣子取证数据归集与压缩打包器接口
    /// </summary>
    public interface IFlightPackCollector
    {
        /// <summary>
        /// 收集关联数据并生成 .rcspack (ZIP 字节数组)
        /// 收集关联多源数据（API 报文、操作记录、系统异常）并生成 .rcspack (ZIP 字节数组)
        /// </summary>
        /// <param name="request">排障打包请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>压缩包二进制字节流</returns>
        Task<byte[]> CollectAndPackAsync(FlightPackRequest request, CancellationToken cancellationToken = default);
    }
}
