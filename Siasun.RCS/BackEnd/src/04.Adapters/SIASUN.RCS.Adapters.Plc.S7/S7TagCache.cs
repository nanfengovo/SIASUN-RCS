using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Volo.Abp.DependencyInjection;

namespace SIASUN.RCS.Adapters.Plc.S7
{
    /// <summary>
    /// 西门子 S7 PLC 内存标签高速缓存（支持无锁读取、批量更新与 TTL 时效性校验）
    /// </summary>
    public class S7TagCache : ISingletonDependency
    {
        private readonly ConcurrentDictionary<string, S7CachedTag> _tags = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 更新或插入单个 PLC 标签数据
        /// </summary>
        /// <param name="tagName">标签名称（例如 "DB100.DBX0.0" 或 "ERACK_SLOT_01"）</param>
        /// <param name="value">标签原始值</param>
        /// <param name="qualityOk">通信质量指示</param>
        public void SetTag(string tagName, object? value, bool qualityOk = true)
        {
            _tags[tagName] = new S7CachedTag(tagName, value, DateTime.UtcNow, qualityOk);
        }

        /// <summary>
        /// 批量更新槽位缓存
        /// </summary>
        /// <param name="slots">槽位数据列表</param>
        public void BatchUpdateSlots(IEnumerable<S7SlotData> slots)
        {
            var now = DateTime.UtcNow;
            foreach (var slot in slots)
            {
                var key = $"SLOT_{slot.SlotIndex:D3}";
                _tags[key] = new S7CachedTag(key, slot, now, true);
            }
        }

        /// <summary>
        /// 获取标签缓存值
        /// </summary>
        /// <typeparam name="T">期望类型</typeparam>
        /// <param name="tagName">标签名</param>
        /// <param name="maxAge">允许最大缓存时限（默认 5 秒）</param>
        /// <returns>值与是否存在</returns>
        public (bool Found, T? Value) TryGetTag<T>(string tagName, TimeSpan? maxAge = null)
        {
            if (!_tags.TryGetValue(tagName, out var cached))
            {
                return (false, default);
            }

            var limit = maxAge ?? TimeSpan.FromSeconds(5);
            if (DateTime.UtcNow - cached.UpdatedAt > limit)
            {
                return (false, default); // 缓存已过期
            }

            if (cached.Value is T typedVal)
            {
                return (true, typedVal);
            }

            return (false, default);
        }

        /// <summary>
        /// 获取所有当前缓存快照
        /// </summary>
        public IReadOnlyDictionary<string, S7CachedTag> GetSnapshot() => _tags;
    }

    /// <summary>
    /// PLC 缓存标签包装
    /// </summary>
    public record S7CachedTag(
        string TagName,
        object? Value,
        DateTime UpdatedAt,
        bool QualityOk);
}
