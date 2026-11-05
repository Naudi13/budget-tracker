using BudTack.Identity;
using BudTack.Identity.Api;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BudTack.Unit.Tests.Identity;

public sealed class IdentityModuleTests
{
    [Fact]
    public void AddIdentityModule_registered_resolvesOneFacadePerScope()
    {
        using var provider = TestModules.HostServices().AddIdentityModule(TestModules.Identity).BuildServiceProvider();

        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        var facade = first.ServiceProvider.GetRequiredService<IdentityFacade>();

        Assert.Same(facade, first.ServiceProvider.GetRequiredService<IdentityFacade>());
        Assert.NotSame(facade, second.ServiceProvider.GetRequiredService<IdentityFacade>());
    }
}
