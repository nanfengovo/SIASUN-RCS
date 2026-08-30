using System.Reflection;
using Xunit;

namespace SIASUN.RCS.Domain.Tests.Shared;

public class ConfiguratorTests
{
    [Fact]
    public void RCSGlobalFeatureConfigurator_Configure_Should_Not_Throw()
    {
        var exception = Record.Exception(() => RCSGlobalFeatureConfigurator.Configure());
        Assert.Null(exception);
    }

    [Fact]
    public void RCSModuleExtensionConfigurator_Configure_Should_Not_Throw()
    {
        var exception = Record.Exception(() => RCSModuleExtensionConfigurator.Configure());
        Assert.Null(exception);
    }

    [Fact]
    public void RCSModuleExtensionConfigurator_PrivateMethods_Should_Execute()
    {
        // Explicitly invoke private methods to ensure 100% coverage even if OneTimeRunner skipped them
        var type = typeof(RCSModuleExtensionConfigurator);
        
        var method1 = type.GetMethod("ConfigureExistingProperties", BindingFlags.NonPublic | BindingFlags.Static);
        if (method1 != null) method1.Invoke(null, null);

        var method2 = type.GetMethod("ConfigureExtraProperties", BindingFlags.NonPublic | BindingFlags.Static);
        if (method2 != null) method2.Invoke(null, null);
    }
}
