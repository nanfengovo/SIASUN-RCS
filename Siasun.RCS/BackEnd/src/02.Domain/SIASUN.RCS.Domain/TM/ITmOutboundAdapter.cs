using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.TM
{
    /// <summary>
    /// TM (Transport Manager / 底盘控制系统) 出站适配器契约（六边形架构端口）
    /// 隔离底层 TM 物理网络通信，负责组装 OptionCode 并注册 TM 序列号双向映射
    /// </summary>
    public interface ITmOutboundAdapter : ITransientDependency
    {
        /// <summary>
        /// 向 TM 派发指定航段动作（携带 OptionCode 并自动在注册表中完成绑定）
        /// </summary>
        /// <param name="context">下发上下文</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>派发结果</returns>
        Task<TmDispatchResult> DispatchLegAsync(TmDispatchContext context, CancellationToken cancellationToken = default);
    }
}
