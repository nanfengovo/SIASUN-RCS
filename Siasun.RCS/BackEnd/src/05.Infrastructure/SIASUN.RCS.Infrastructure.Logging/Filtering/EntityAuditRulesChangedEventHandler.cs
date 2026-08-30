using System.Threading.Tasks;
using SIASUN.RCS.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace SIASUN.RCS.Infrastructure.Logging.Filtering
{
    public class EntityAuditRulesChangedEventHandler :
        ILocalEventHandler<EntityAuditRulesChangedEvent>,
        ITransientDependency
    {
        private readonly IEntityAuditRuleEvaluator _evaluator;

        public EntityAuditRulesChangedEventHandler(IEntityAuditRuleEvaluator evaluator)
        {
            _evaluator = evaluator;
        }

        public async Task HandleEventAsync(EntityAuditRulesChangedEvent eventData)
        {
            await _evaluator.RefreshRulesAsync();
        }
    }
}
