using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace SIASUN.RCS.Auditing
{
    public class AuditLogFilterRule : FullAuditedAggregateRoot<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string PathPattern { get; set; } = string.Empty;
        public FilterRuleType RuleType { get; set; }
        public FilterDirection Direction { get; set; }
        public string HttpMethod { get; set; } = "*";
        public bool IsEnabled { get; set; } = true;
        public string? Description { get; set; }

        public AuditLogFilterRule()
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
    }
}
