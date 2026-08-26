using System.Threading.Tasks;
using SIASUN.RCS.Auditing;

namespace SIASUN.RCS.Infrastructure.Logging.Filtering
{
    public interface IAuditLogFilterEvaluator
    {
        Task InitializeAsync();
        bool ShouldAudit(string path, string httpMethod, Direction direction);
        Task RefreshRulesAsync();
    }
}
