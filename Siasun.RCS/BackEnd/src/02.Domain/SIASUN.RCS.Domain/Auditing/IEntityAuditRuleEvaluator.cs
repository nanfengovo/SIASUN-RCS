using System.Threading.Tasks;

namespace SIASUN.RCS.Auditing
{
    public record EntityAuditRuleResult(EntityAuditMode Mode, int SampleIntervalMs, string? ExcludedProperties);

    public interface IEntityAuditRuleEvaluator
    {
        EntityAuditRuleResult Evaluate(string fullName, string shortName);
        Task RefreshRulesAsync();
    }
}
