using SIASUN.RCS.Samples;
using Xunit;

namespace SIASUN.RCS.EntityFrameworkCore.Applications;

[Collection(RCSTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<RCSEntityFrameworkCoreTestModule>
{

}
