using System.Collections.Generic;

namespace SIASUN.RCS.Infrastructure.Logging.Diagnostics.SignalR
{
    /// <summary>
    /// 诊断事件实时流式分发代理中台契约
    /// </summary>
    public interface IDiagnosticLiveStreamBroker
    {
        /// <summary>
        /// 是否启用了推流中台
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// 向中台发布一条新的实时时序事件 (自动路由至对应主题并存入环形缓冲区)
        /// </summary>
        /// <param name="evt">时序事件 DTO</param>
        void Publish(LiveEventDto evt);

        /// <summary>
        /// 获取指定主题的最近环形历史事件缓存（用于新连入客户端快速回填上下文）
        /// </summary>
        /// <param name="topic">主题名 (all, errors, task:{id}, vehicle:{id})</param>
        /// <param name="maxCount">最多获取条数</param>
        /// <returns>历史事件只读列表</returns>
        IReadOnlyList<LiveEventDto> GetHistory(string topic, int? maxCount = null);

        /// <summary>
        /// 取出当前积攒待推流的所有批次字典并清空待发队列（由后台节流 Worker 周期调用）
        /// </summary>
        /// <returns>按主题分组的事件批次列表字典</returns>
        Dictionary<string, List<LiveEventDto>> DequeuePendingBatches();
    }
}
