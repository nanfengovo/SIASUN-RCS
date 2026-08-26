using SIASUN.RCS.Auditing;
using Xunit;

namespace SIASUN.RCS.EntityFrameworkCore.Applications
{
    [Collection(RCSTestConsts.CollectionDefinitionName)]
    public class AuditLogFilterRuleAppServiceTests : AuditLogFilterRuleAppServiceTests<RCSEntityFrameworkCoreTestModule>
    {
    }
}
