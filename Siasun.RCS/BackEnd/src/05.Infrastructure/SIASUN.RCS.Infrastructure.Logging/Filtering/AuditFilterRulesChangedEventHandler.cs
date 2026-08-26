using System.Threading.Tasks;
using SIASUN.RCS.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace SIASUN.RCS.Infrastructure.Logging.Filtering
{
    public class AuditFilterRulesChangedEventHandler :
        ILocalEventHandler<AuditFilterRulesChangedEvent>,
        ITransientDependency
    {
        private readonly IAuditLogFilterEvaluator _evaluator;

        public AuditFilterRulesChangedEventHandler(IAuditLogFilterEvaluator evaluator)
        {
            _evaluator = evaluator;
        }

        public async Task HandleEventAsync(AuditFilterRulesChangedEvent eventData)
        {
            await _evaluator.RefreshRulesAsync();
        }
    }
}
