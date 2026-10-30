namespace GeoConquest.Core.Tests;

public sealed class SolutionSmokeTests
{
    [Fact]
    public void CoreAssembly_WhenLoaded_HasExpectedName()
    {
        var assemblyName = typeof(Constants.AssemblyMarker).Assembly.GetName().Name;

        Assert.Equal("GeoConquest.Core", assemblyName);
    }
}
