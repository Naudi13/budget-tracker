using BudTack.Identity;
using BudTack.ModuleIntegration.Tests.Support;
using BudTack.Transactions;
using BudTack.Transactions.Api;
using BudTack.Transactions.Api.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BudTack.ModuleIntegration.Tests.Transactions;

/// <summary>
/// HU-07 through <see cref="TransactionsFacade"/> only, against a real
/// PostgreSQL: the period rule (TC-007), changes held to the same rules and a
/// physical delete (BR-06, TC-019), and a category blocked while it has a
/// budget (BR-05).
/// </summary>
[Collection(PostgresTestGroup.Name)]
public sealed class BudgetsFacadeTests(PostgresFixture database) : IAsyncLifetime, IDisposable
{
    private static readonly Guid Ana = Guid.Parse("00000000-0000-7000-8000-00000000000a");
    private static readonly DateOnly Nov1 = new(2026, 11, 1);
    private static readonly DateOnly Nov30 = new(2026, 11, 30);

    private ServiceProvider _services = null!;
    private Category _office = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await database.ResetAsync();
        _services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(database.DataSource)
            .AddIdentityModule(new IdentitySettings("module-integration-secret-0123456789abc", TimeSpan.FromHours(1)))
            .AddTransactionsModule()
            .BuildServiceProvider();
        _office = await Facade().CreateCategoryAsync(Ana, new NewCategory("Oficina", "expense"), Ct);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public void Dispose() => _services.Dispose();

    private TransactionsFacade Facade() => _services.CreateScope().ServiceProvider.GetRequiredService<TransactionsFacade>();

    [Fact]
    public async Task CreateBudgetAsync_periodEndingBeforeItStarts_storesNothing() // TC-007
    {
        await Assert.ThrowsAsync<TransactionsBusinessRuleException>(
            () => Facade().CreateBudgetAsync(Ana, new NewBudget(_office.Id, 500m, Nov1, new DateOnly(2026, 10, 31)), Ct));

        Assert.Equal(0L, (await Facade().ListBudgetsAsync(cancellationToken: Ct)).Meta.Total);
    }

    [Fact]
    public async Task TheFiveSteps_ofTC019()
    {
        var budget = await Facade().CreateBudgetAsync(Ana, new NewBudget(_office.Id, 500m, Nov1, Nov30), Ct);

        // 1. A change that breaks the period fails, and the row keeps its end.
        await Assert.ThrowsAsync<TransactionsBusinessRuleException>(
            () => Facade().UpdateBudgetAsync(Ana, budget.Id, new BudgetChanges(null, null, null, new DateOnly(2026, 10, 15)), Ct));
        Assert.Equal(Nov30, (await Facade().ListBudgetsAsync(cancellationToken: Ct)).Data.Single().PeriodEnd);

        // 2. Changing the amount succeeds.
        Assert.Equal(650m, (await Facade().UpdateBudgetAsync(Ana, budget.Id, new BudgetChanges(null, 650m, null, null), Ct)).Amount);
        Assert.Equal(650m, (await Facade().ListBudgetsAsync(cancellationToken: Ct)).Data.Single().Amount);

        // 3. The category cannot be deleted: it owns a budget (BR-05).
        await Assert.ThrowsAsync<TransactionsConflictException>(() => Facade().DeleteCategoryAsync(_office.Id, Ct));

        // 4. The budget is deleted, physically (BR-06).
        await Facade().DeleteBudgetAsync(budget.Id, Ct);
        await using (var command = database.DataSource.CreateCommand("SELECT count(*) FROM transactions.budgets"))
        {
            Assert.Equal(0L, (long)(await command.ExecuteScalarAsync(Ct))!);
        }

        // 5. Nothing else blocks the category now.
        await Facade().DeleteCategoryAsync(_office.Id, Ct);
    }

    [Fact]
    public async Task ListBudgetsAsync_byCategory_newestPeriodFirst()
    {
        var travel = await Facade().CreateCategoryAsync(Ana, new NewCategory("Viajes", "expense"), Ct);
        await Facade().CreateBudgetAsync(Ana, new NewBudget(_office.Id, 1m, Nov1, Nov30), Ct);
        await Facade().CreateBudgetAsync(Ana, new NewBudget(_office.Id, 2m, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 31)), Ct);
        await Facade().CreateBudgetAsync(Ana, new NewBudget(travel.Id, 3m, Nov1, Nov30), Ct);

        var office = await Facade().ListBudgetsAsync(categoryId: _office.Id, cancellationToken: Ct);
        var all = await Facade().ListBudgetsAsync(cancellationToken: Ct);

        Assert.Equal([2m, 1m], office.Data.Select(b => b.Amount));
        Assert.Equal(2L, office.Meta.Total);
        Assert.Equal(3L, all.Meta.Total);
    }

    [Fact]
    public async Task Budgets_ofAnInactiveCategory_cannotBeSetOrChanged() // BR-05
    {
        var budget = await Facade().CreateBudgetAsync(Ana, new NewBudget(_office.Id, 500m, Nov1, Nov30), Ct);
        await Facade().UpdateCategoryAsync(Ana, _office.Id, new CategoryChanges(null, false), Ct);

        await Assert.ThrowsAsync<TransactionsBusinessRuleException>(
            () => Facade().CreateBudgetAsync(Ana, new NewBudget(_office.Id, 1m, Nov1, Nov30), Ct));
        await Assert.ThrowsAsync<TransactionsBusinessRuleException>(
            () => Facade().UpdateBudgetAsync(Ana, budget.Id, new BudgetChanges(null, 1m, null, null), Ct));
    }

}
