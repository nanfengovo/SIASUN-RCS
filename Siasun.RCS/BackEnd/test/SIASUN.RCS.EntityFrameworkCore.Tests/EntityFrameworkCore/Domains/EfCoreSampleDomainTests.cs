using SIASUN.RCS.Samples;
using Xunit;

namespace SIASUN.RCS.EntityFrameworkCore.Domains;

[Collection(RCSTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<RCSEntityFrameworkCoreTestModule>
{

}
