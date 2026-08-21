using Xunit;

namespace SIASUN.RCS.EntityFrameworkCore;

[CollectionDefinition(RCSTestConsts.CollectionDefinitionName)]
public class RCSEntityFrameworkCoreCollection : ICollectionFixture<RCSEntityFrameworkCoreFixture>
{

}
