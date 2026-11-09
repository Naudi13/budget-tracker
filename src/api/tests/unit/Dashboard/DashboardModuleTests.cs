using BudTack.Dashboard;
using BudTack.Dashboard.Api;
using BudTack.Identity;
using BudTack.Transactions;
using BudTack.Transactions.Api;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BudTack.Unit.Tests.Dashboard;

public sealed class DashboardModuleTests
{
    private static ServiceProvider AllModules() =>
        TestModules.HostServices()
            .AddIdentityModule(TestModules.Identity)
            .AddTransactionsModule()
            .AddDashboardModule()
            .BuildServiceProvider();

    [Fact]
    public void AddDashboardModule_afterTransactions_resolvesOneFacadePerScope()
    {
        using var provider = AllModules();

        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        var facade = first.ServiceProvider.GetRequiredService<DashboardFacade>();

        Assert.Same(facade, first.ServiceProvider.GetRequiredService<DashboardFacade>());
        Assert.NotSame(facade, second.ServiceProvider.GetRequiredService<DashboardFacade>());
    }

    [Fact]
    public void AddDashboardModule_resolved_usesTheTransactionsFacadeOfItsScope()
    {
        using var provider = AllModules();
        using var scope = provider.CreateScope();

        var facade = scope.ServiceProvider.GetRequiredService<DashboardFacade>();

        Assert.Same(scope.ServiceProvider.GetRequiredService<TransactionsFacade>(), facade.Transactions);
    }

    [Fact]
    public void AddDashboardModule_withoutTransactions_failsToResolve()
    {
        using var provider = new ServiceCollection().AddDashboardModule().BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.Throws<InvalidOperationException>(
            () => scope.ServiceProvider.GetRequiredService<DashboardFacade>());
    }
}
