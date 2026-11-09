using BudTack.Dashboard;
using BudTack.Dashboard.Api;
using BudTack.Identity;
using BudTack.ModuleIntegration.Tests.Support;
using BudTack.Transactions;
using BudTack.Transactions.Api;
using BudTack.Transactions.Api.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BudTack.ModuleIntegration.Tests.Dashboard;

/// <summary>
/// HU-08 through <see cref="DashboardFacade"/> only, against a real PostgreSQL:
/// totals computed on read (BR-03), deleted transactions left out (BR-04), and
/// the five most recent transactions.
/// </summary>
[Collection(PostgresTestGroup.Name)]
public sealed class DashboardFacadeTests(PostgresFixture database) : IAsyncLifetime, IDisposable
{
    private static readonly Guid Ana = Guid.Parse("00000000-0000-7000-8000-00000000000a");
    private static readonly Guid Leo = Guid.Parse("00000000-0000-7000-8000-00000000000b");

    private ServiceProvider _services = null!;
    private Guid _supplies;
    private Guid _store;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await database.ResetAsync();
        _services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(database.DataSource)
            .AddIdentityModule(new IdentitySettings("module-integration-secret-0123456789abc", TimeSpan.FromHours(1)))
            .AddTransactionsModule()
            .AddDashboardModule()
            .BuildServiceProvider();

        var office = await Transactions().CreateCategoryAsync(Ana, new NewCategory("Oficina", "expense"), Ct);
        _supplies = (await Transactions().CreateSubcategoryAsync(Ana, office.Id, new NewSubcategory("Papelería"), Ct)).Id;
        var sales = await Transactions().CreateCategoryAsync(Ana, new NewCategory("Ventas", "income"), Ct);
        _store = (await Transactions().CreateSubcategoryAsync(Ana, sales.Id, new NewSubcategory("Tienda"), Ct)).Id;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public void Dispose() => _services.Dispose();

    private TransactionsFacade Transactions() => _services.CreateScope().ServiceProvider.GetRequiredService<TransactionsFacade>();

    private DashboardFacade Dashboard() => _services.CreateScope().ServiceProvider.GetRequiredService<DashboardFacade>();

    private Task<Transaction> Record(DateOnly date, string description, Guid subcategory, params decimal[] amounts) =>
        Transactions().RecordTransactionAsync(
            Ana, new NewTransaction(date, description, [.. amounts.Select(a => new NewTransactionDetail(subcategory, a))]), Ct);

    [Fact]
    public async Task GetDashboardAsync_noTransactions_isAllZeros()
    {
        var dashboard = await Dashboard().GetDashboardAsync(new Viewer(Ana, false), Ct);

        Assert.Equal((0m, 0m, 0m), (dashboard.TotalIncome, dashboard.TotalExpenses, dashboard.NetBalance));
        Assert.Empty(dashboard.RecentTransactions);
    }

    [Fact]
    public async Task GetDashboardAsync_addsUpOnRead_withoutDeletedOnes() // TC-008, BR-03, BR-04
    {
        await Record(new DateOnly(2026, 11, 1), "Venta", _store, 600m, 400m);
        await Record(new DateOnly(2026, 11, 2), "Papel", _supplies, 300m, 50m);
        var mistake = await Record(new DateOnly(2026, 11, 3), "Error", _supplies, 100m);
        await Transactions().DeleteTransactionAsync(new Viewer(Ana, false), mistake.Id, new TransactionDeletion("Monto equivocado"), Ct);

        var dashboard = await Dashboard().GetDashboardAsync(new Viewer(Ana, false), Ct);

        Assert.Equal((1000m, 350m, 650m), (dashboard.TotalIncome, dashboard.TotalExpenses, dashboard.NetBalance));
        Assert.DoesNotContain(dashboard.RecentTransactions, t => t.Id == mistake.Id);
    }

    [Fact]
    public async Task GetDashboardAsync_aNewExpense_isReflectedOnTheNextRead() // HU-08, BR-03
    {
        await Record(new DateOnly(2026, 11, 1), "Venta", _store, 1000m);
        await Record(new DateOnly(2026, 11, 2), "Papel", _supplies, 400m);
        Assert.Equal(600m, (await Dashboard().GetDashboardAsync(new Viewer(Ana, false), Ct)).NetBalance);

        await Record(new DateOnly(2026, 11, 3), "Tinta", _supplies, 100m);

        Assert.Equal(500m, (await Dashboard().GetDashboardAsync(new Viewer(Ana, false), Ct)).NetBalance);
    }

    [Fact]
    public async Task GetDashboardAsync_showsTheFiveNewest_newestFirst()
    {
        for (var day = 1; day <= 8; day++)
        {
            await Record(new DateOnly(2026, 11, day), $"Día {day}", _supplies, day);
        }

        var dashboard = await Dashboard().GetDashboardAsync(new Viewer(Ana, false), Ct);

        Assert.Equal(["Día 8", "Día 7", "Día 6", "Día 5", "Día 4"], dashboard.RecentTransactions.Select(t => t.Description));
        Assert.Equal(36m, dashboard.TotalExpenses);
    }

    [Fact]
    public async Task GetDashboardAsync_ofAnotherBusiness_seesNoneOfMine()
    {
        await Record(new DateOnly(2026, 11, 1), "Venta", _store, 1000m);

        var dashboard = await Dashboard().GetDashboardAsync(new Viewer(Leo, false), Ct);

        Assert.Equal(0m, dashboard.TotalIncome);
        Assert.Empty(dashboard.RecentTransactions);
    }
}
