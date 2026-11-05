using BudTack.Identity;
using BudTack.Identity.Api;
using BudTack.Transactions;
using BudTack.Transactions.Api;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BudTack.Unit.Tests.Transactions;

public sealed class TransactionsModuleTests
{
    [Fact]
    public void AddTransactionsModule_afterIdentity_resolvesOneFacadePerScope()
    {
        using var provider = TestModules.HostServices()
            .AddIdentityModule(TestModules.Identity)
            .AddTransactionsModule()
            .BuildServiceProvider();

        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        var facade = first.ServiceProvider.GetRequiredService<TransactionsFacade>();

        Assert.Same(facade, first.ServiceProvider.GetRequiredService<TransactionsFacade>());
        Assert.NotSame(facade, second.ServiceProvider.GetRequiredService<TransactionsFacade>());
    }

    [Fact]
    public void AddTransactionsModule_resolved_usesTheIdentityFacadeOfItsScope()
    {
        using var provider = TestModules.HostServices()
            .AddIdentityModule(TestModules.Identity)
            .AddTransactionsModule()
            .BuildServiceProvider();
        using var scope = provider.CreateScope();

        var facade = scope.ServiceProvider.GetRequiredService<TransactionsFacade>();

        Assert.Same(scope.ServiceProvider.GetRequiredService<IdentityFacade>(), facade.Identity);
    }

    [Fact]
    public void AddTransactionsModule_withoutIdentity_failsToResolve()
    {
        using var provider = new ServiceCollection().AddTransactionsModule().BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.Throws<InvalidOperationException>(
            () => scope.ServiceProvider.GetRequiredService<TransactionsFacade>());
    }
}
