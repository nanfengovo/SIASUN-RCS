using System.Threading.Tasks;
using SIASUN.RCS.TM.Dtos;
using Volo.Abp.Application.Services;

namespace SIASUN.RCS.TM
{
    /// <summary>
    /// TM 报文状态回调应用服务接口（入站六边形端口）
    /// 接收底层小车/TM 的动作完成、失败与告警报文，通过 TaskSerialRegistry 精准反查内部任务并唤醒工作流
    /// </summary>
    public interface ITmCallbackAppService : IApplicationService
    {
        /// <summary>
        /// 处理 TM 报文异步回调（消除字符串替换 hack，准确驱动多程段流转）
        /// </summary>
        /// <param name="input">TM 回调报文入参</param>
        /// <returns>回调处理与步进流转结果</returns>
        Task<TmCallbackResultDto> HandleCallbackAsync(TmCallbackInput input);
    }
}
