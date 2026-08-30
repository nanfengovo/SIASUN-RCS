using NSubstitute;
using SIASUN.RCS.Identity;
using Volo.Abp.Identity.Settings;
using Volo.Abp.Settings;
using Xunit;

namespace SIASUN.RCS.Domain.Tests.Identity;

public class ChangeIdentityPasswordPolicySettingDefinitionProviderTests
{
    [Fact]
    public void Define_Should_Change_Password_Policy_Defaults()
    {
        // Arrange
        var context = Substitute.For<ISettingDefinitionContext>();
        
        var setting1 = new SettingDefinition(IdentitySettingNames.Password.RequireNonAlphanumeric, "true");
        var setting2 = new SettingDefinition(IdentitySettingNames.Password.RequireLowercase, "true");
        var setting3 = new SettingDefinition(IdentitySettingNames.Password.RequireUppercase, "true");
        var setting4 = new SettingDefinition(IdentitySettingNames.Password.RequireDigit, "true");

        context.GetOrNull(IdentitySettingNames.Password.RequireNonAlphanumeric).Returns(setting1);
        context.GetOrNull(IdentitySettingNames.Password.RequireLowercase).Returns(setting2);
        context.GetOrNull(IdentitySettingNames.Password.RequireUppercase).Returns(setting3);
        context.GetOrNull(IdentitySettingNames.Password.RequireDigit).Returns(setting4);

        var provider = new ChangeIdentityPasswordPolicySettingDefinitionProvider();

        // Act
        provider.Define(context);

        // Assert
        Assert.Equal("False", setting1.DefaultValue);
        Assert.Equal("False", setting2.DefaultValue);
        Assert.Equal("False", setting3.DefaultValue);
        Assert.Equal("False", setting4.DefaultValue);
    }
}
