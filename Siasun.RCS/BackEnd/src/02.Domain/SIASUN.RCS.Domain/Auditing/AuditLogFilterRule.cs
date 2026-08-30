using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace SIASUN.RCS.Auditing
{
    public class AuditLogFilterRule : FullAuditedAggregateRoot<Guid>
    {
        public string Name { get; private set; } = string.Empty;
        public string PathPattern { get; private set; } = string.Empty;
        public FilterRuleType RuleType { get; private set; }
        public FilterDirection Direction { get; private set; }
        public string HttpMethod { get; private set; } = "*";
        public bool IsEnabled { get; private set; } = true;
        public string? Description { get; private set; }

        protected AuditLogFilterRule()
        {
        }

        public AuditLogFilterRule(
            Guid id,
            string name,
            string pathPattern,
            FilterRuleType ruleType = FilterRuleType.Whitelist,
            FilterDirection direction = FilterDirection.Both,
            string httpMethod = "*",
            bool isEnabled = true,
            string? description = null) : base(id)
        {
            Name = name;
            PathPattern = pathPattern;
            RuleType = ruleType;
            Direction = direction;
            HttpMethod = string.IsNullOrWhiteSpace(httpMethod) ? "*" : httpMethod.ToUpperInvariant();
            IsEnabled = isEnabled;
            Description = description;
        }

        public void Update(string name, string pathPattern, FilterRuleType ruleType, FilterDirection direction, string httpMethod, bool isEnabled, string? description)
        {
            Name = name;
            PathPattern = pathPattern;
            RuleType = ruleType;
            Direction = direction;
            HttpMethod = string.IsNullOrWhiteSpace(httpMethod) ? "*" : httpMethod.ToUpperInvariant();
            IsEnabled = isEnabled;
            Description = description;
        }

        public void Toggle()
        {
            IsEnabled = !IsEnabled;
        }
    }
}
