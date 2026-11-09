using BudTack.Transactions.Api.Contracts;
using BudTack.Transactions.Internal.Application;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using DomainCategory = BudTack.Transactions.Internal.Domain.Category;

namespace BudTack.Unit.Tests.Transactions;

/// <summary>HU-07 use cases against the tables in memory.</summary>
public sealed class ManageBudgetsTests
{
    private static readonly Guid Ana = Guid.Parse("00000000-0000-7000-8000-00000000000a");
    private static readonly Guid Office = Guid.Parse("00000000-0000-7000-8000-0000000000c1");
    private static readonly Guid Travel = Guid.Parse("00000000-0000-7000-8000-0000000000c2");
    private static readonly Guid Old = Guid.Parse("00000000-0000-7000-8000-0000000000c3");
    private static readonly DateOnly Nov1 = new(2026, 11, 1);
    private static readonly DateOnly Nov30 = new(2026, 11, 30);

    private readonly InMemoryBudgets _budgets = new();
    private readonly InMemoryCategories _categories = new();
    private readonly ManageBudgets _manage;

    public ManageBudgetsTests()
    {
        _categories.Categories.Add(new DomainCategory(Office, "Oficina", "expense", IsActive: true));
        _categories.Categories.Add(new DomainCategory(Travel, "Viajes", "expense", IsActive: true));
        _categories.Categories.Add(new DomainCategory(Old, "Viejos", "expense", IsActive: false));
        _manage = new ManageBudgets(_budgets, _categories, NullLogger<ManageBudgets>.Instance);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private Task<Budget> Create(Guid category, decimal amount, DateOnly start, DateOnly end, Guid? owner = null) =>
        _manage.CreateAsync(owner ?? Ana, new NewBudget(category, amount, start, end), Ct);

    [Fact]
    public async Task CreateAsync_activeCategory_isStored()
    {
        var budget = await Create(Office, 500m, Nov1, Nov30);

        Assert.Equal(new Budget(budget.Id, Office, 500m, Nov1, Nov30), budget);
    }

    [Fact]
    public async Task CreateAsync_inactiveCategory_storesNothing() // BR-05
    {
        await Assert.ThrowsAsync<TransactionsBusinessRuleException>(() => Create(Old, 500m, Nov1, Nov30));
        Assert.Empty(_budgets.Rows);
    }

    [Fact]
    public async Task ListAsync_newestPeriodFirst_andByCategory()
    {
        await Create(Office, 1m, Nov1, Nov30);
        await Create(Office, 2m, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 31));
        await Create(Travel, 3m, Nov1, Nov30);

        var all = await _manage.ListAsync(null, null, null, Ct);
        var office = await _manage.ListAsync(null, null, Office, Ct);

        Assert.Equal(2m, all.Data[0].Amount);
        Assert.Equal(3L, all.Meta.Total);
        Assert.Equal([2m, 1m], office.Data.Select(b => b.Amount));
    }

    [Fact]
    public async Task UpdateAsync_amount_isChanged() // BR-06
    {
        var budget = await Create(Office, 500m, Nov1, Nov30);

        var changed = await _manage.UpdateAsync(Ana, budget.Id, new BudgetChanges(null, 650m, null, null), Ct);

        Assert.Equal(650m, changed.Amount);
        Assert.Equal(650m, Assert.Single(_budgets.Rows).Amount);
    }

    [Fact]
    public async Task UpdateAsync_breakingThePeriod_keepsThePreviousDates() // BR-06, TC-019
    {
        var budget = await Create(Office, 500m, Nov1, Nov30);

        await Assert.ThrowsAsync<TransactionsBusinessRuleException>(
            () => _manage.UpdateAsync(Ana, budget.Id, new BudgetChanges(null, null, null, new DateOnly(2026, 10, 15)), Ct));
        Assert.Equal(Nov30, Assert.Single(_budgets.Rows).PeriodEnd);
    }

    [Fact]
    public async Task UpdateAsync_toAnInactiveCategory_isRefused() // BR-05
    {
        var budget = await Create(Office, 500m, Nov1, Nov30);

        await Assert.ThrowsAsync<TransactionsBusinessRuleException>(
            () => _manage.UpdateAsync(Ana, budget.Id, new BudgetChanges(Old, null, null, null), Ct));
        Assert.Equal(Office, Assert.Single(_budgets.Rows).CategoryId);
    }

    [Fact]
    public async Task DeleteAsync_removesTheRow_thenIsNotFound() // BR-06
    {
        var budget = await Create(Office, 500m, Nov1, Nov30);

        await _manage.DeleteAsync(budget.Id, Ct);

        Assert.Empty(_budgets.Rows);
        var error = await Assert.ThrowsAsync<TransactionsNotFoundException>(() => _manage.DeleteAsync(budget.Id, Ct));
        Assert.Equal(ManageBudgets.NotFound, error.Message);
    }
}
