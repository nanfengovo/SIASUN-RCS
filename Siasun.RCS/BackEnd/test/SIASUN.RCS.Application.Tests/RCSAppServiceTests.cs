using Shouldly;
using Xunit;
using SIASUN.RCS.Localization;

namespace SIASUN.RCS;

public class RCSAppServiceTests
{
    public class TestAppService : RCSAppService
    {
        public System.Type GetLocalizationResource() => LocalizationResource;
    }

    [Fact]
    public void Should_Set_LocalizationResource()
    {
        var service = new TestAppService();
        service.GetLocalizationResource().ShouldBe(typeof(RCSResource));
    }
}
