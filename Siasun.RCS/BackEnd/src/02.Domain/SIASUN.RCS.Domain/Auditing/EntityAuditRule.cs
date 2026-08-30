using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace SIASUN.RCS.Auditing
{
    /// <summary>
    /// 实体审计策略配置规则 (富聚合根)
    /// </summary>
    public class EntityAuditRule : FullAuditedAggregateRoot<Guid>
    {
        public string Name { get; private set; } = string.Empty;
        public string EntityTypePattern { get; private set; } = string.Empty;
        public EntityAuditMode Mode { get; private set; }
        public int SampleIntervalMs { get; private set; }
        public string? ExcludedProperties { get; private set; }
        public int Priority { get; private set; }
        public bool IsEnabled { get; private set; }

        protected EntityAuditRule() { } // EF Core 需要

        public EntityAuditRule(
            Guid id, 
            string name, 
            string entityTypePattern, 
            EntityAuditMode mode, 
            int sampleIntervalMs, 
            string? excludedProperties, 
            int priority, 
            bool isEnabled) : base(id)
        {
            SetName(name);
            SetPattern(entityTypePattern);
            Mode = mode;
            SetSampleIntervalMs(sampleIntervalMs);
            ExcludedProperties = excludedProperties;
            Priority = priority;
            IsEnabled = isEnabled;
        }

        public void Update(string name, string entityTypePattern, EntityAuditMode mode, int sampleIntervalMs, string? excludedProperties, int priority)
        {
            SetName(name);
            SetPattern(entityTypePattern);
            Mode = mode;
            SetSampleIntervalMs(sampleIntervalMs);
            ExcludedProperties = excludedProperties;
            Priority = priority;
        }

        public void Enable()
        {
            IsEnabled = true;
        }

        public void Disable()
        {
            IsEnabled = false;
        }

        public void Toggle()
        {
            IsEnabled = !IsEnabled;
        }

        private void SetName(string name)
        {
            Check.NotNullOrWhiteSpace(name, nameof(name), EntityAuditRuleConsts.MaxNameLength);
            Name = name;
        }

        private void SetPattern(string pattern)
        {
            Check.NotNullOrWhiteSpace(pattern, nameof(pattern), EntityAuditRuleConsts.MaxEntityTypePatternLength);
            EntityTypePattern = pattern;
        }

        private void SetSampleIntervalMs(int ms)
        {
            if (ms < 0) throw new ArgumentException("SampleIntervalMs must be >= 0", nameof(ms));
            SampleIntervalMs = ms;
        }
    }
}
