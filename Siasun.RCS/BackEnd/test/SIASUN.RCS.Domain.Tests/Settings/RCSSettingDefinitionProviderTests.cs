using NSubstitute;
using SIASUN.RCS.Settings;
using Volo.Abp.Settings;
using Xunit;

namespace SIASUN.RCS.Domain.Tests.Settings;

public class RCSSettingDefinitionProviderTests
{
    [Fact]
    public void Define_Should_Not_Throw()
    {
        var context = Substitute.For<ISettingDefinitionContext>();
        var provider = new RCSSettingDefinitionProvider();
        
        var exception = Record.Exception(() => provider.Define(context));
        
        Assert.Null(exception);
    }
}
