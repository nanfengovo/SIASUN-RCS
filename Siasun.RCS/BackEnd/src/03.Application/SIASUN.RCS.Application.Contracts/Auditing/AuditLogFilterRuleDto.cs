using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;

namespace SIASUN.RCS.Auditing
{
    public class AuditLogFilterRuleDto : FullAuditedEntityDto<Guid>, IHasConcurrencyStamp
    {
        public string Name { get; set; } = string.Empty;
        public string PathPattern { get; set; } = string.Empty;
        public FilterRuleType RuleType { get; set; }
        public FilterDirection Direction { get; set; }
        public string HttpMethod { get; set; } = "*";
        public bool IsEnabled { get; set; }
        public string? Description { get; set; }
        public string ConcurrencyStamp { get; set; } = string.Empty;
    }

    public class CreateAuditLogFilterRuleDto
    {
        [Required]
        [MaxLength(AuditLogFilterRuleConsts.MaxNameLength)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(AuditLogFilterRuleConsts.MaxPathPatternLength)]
        public string PathPattern { get; set; } = string.Empty;

        public FilterRuleType RuleType { get; set; } = FilterRuleType.Whitelist;

        public FilterDirection Direction { get; set; } = FilterDirection.Both;

        [MaxLength(AuditLogFilterRuleConsts.MaxHttpMethodLength)]
        public string HttpMethod { get; set; } = "*";

        public bool IsEnabled { get; set; } = true;

        [MaxLength(AuditLogFilterRuleConsts.MaxDescriptionLength)]
        public string? Description { get; set; }
    }

    public class UpdateAuditLogFilterRuleDto : IHasConcurrencyStamp
    {
        [Required]
        [MaxLength(AuditLogFilterRuleConsts.MaxNameLength)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(AuditLogFilterRuleConsts.MaxPathPatternLength)]
        public string PathPattern { get; set; } = string.Empty;

        public FilterRuleType RuleType { get; set; }

        public FilterDirection Direction { get; set; }

        [MaxLength(AuditLogFilterRuleConsts.MaxHttpMethodLength)]
        public string HttpMethod { get; set; } = "*";

        public bool IsEnabled { get; set; }

        [MaxLength(AuditLogFilterRuleConsts.MaxDescriptionLength)]
        public string? Description { get; set; }

        public string ConcurrencyStamp { get; set; } = string.Empty;
    }

    public class GetAuditLogFilterRulesInput : PagedAndSortedResultRequestDto
    {
        public string? Filter { get; set; }
        public FilterRuleType? RuleType { get; set; }
        public FilterDirection? Direction { get; set; }
        public bool? IsEnabled { get; set; }
    }
}
