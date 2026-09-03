using System.Linq;
using Shouldly;
using SIASUN.RCS.Auditing;
using Xunit;

namespace SIASUN.RCS.EntityFrameworkCore.Auditing
{
    public class EfCoreEntityTypeProviderTests : RCSEntityFrameworkCoreTestBase
    {
        private readonly IEntityTypeProvider _provider;

        public EfCoreEntityTypeProviderTests()
        {
            _provider = GetRequiredService<IEntityTypeProvider>();
        }

        [Fact]
        public void GetEntityTypes_ShouldReturnRegisteredEntities()
        {
            var entityTypes = _provider.GetEntityTypes();

            entityTypes.ShouldNotBeNull();
            entityTypes.ShouldNotBeEmpty();
        }
    }
}
