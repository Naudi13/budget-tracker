using BudTack.Transactions.Api.Contracts;
using BudTack.Transactions.Internal.Application;
using BudTack.Transactions.Internal.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BudTack.Unit.Tests.Transactions;

/// <summary>HU-04 to HU-06 use cases against the tables in memory.</summary>
public sealed class ManageMovementsTests
{
    private static readonly Guid Ana = Guid.Parse("00000000-0000-7000-8000-00000000000a");
    private static readonly Guid Leo = Guid.Parse("00000000-0000-7000-8000-00000000000b");
    private static readonly Guid Supplies = Guid.Parse("00000000-0000-7000-8000-0000000000d1");
    private static readonly Guid Sales = Guid.Parse("00000000-0000-7000-8000-0000000000d3");
    private static readonly Guid LeosSubcategory = Guid.Parse("00000000-0000-7000-8000-0000000000d9");

    private readonly InMemoryMovements _tables = new();
    private readonly ManageMovements _movements;

    public ManageMovementsTests()
    {
        _tables.Subcategories[Supplies] = (Ana, new SubcategoryUse(Supplies, "expense", IsUsable: true));
        _tables.Subcategories[Sales] = (Ana, new SubcategoryUse(Sales, "income", IsUsable: true));
        _tables.Subcategories[LeosSubcategory] = (Leo, new SubcategoryUse(LeosSubcategory, "expense", IsUsable: true));
        _movements = new ManageMovements(_tables, NullLogger<ManageMovements>.Instance);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private Task<Transaction> Record(DateOnly date, string description, params (Guid Subcategory, decimal Amount)[] lines) =>
        _movements.RecordAsync(
            Ana,
            new NewTransaction(date, description, [.. lines.Select(l => new NewTransactionDetail(l.Subcategory, l.Amount))]),
            Ct);

    [Fact]
    public async Task RecordAsync_expenseWithOneLine_isStoredWithTheLinesType()
    {
        var transaction = await Record(new DateOnly(2026, 11, 6), "Printer paper", (Supplies, 85.50m));

        var stored = Assert.Single(_tables.Movements);
        Assert.Equal(Ana, stored.OwnedBy);
        Assert.Equal("Printer paper", transaction.Description);
        var line = Assert.Single(transaction.Details);
        Assert.Equal((Supplies, 85.50m, "expense"), (line.SubcategoryId, line.Amount, line.Type));
    }

    [Fact]
    public async Task RecordAsync_mixingIncomeAndExpense_storesNothing() // TC-004
    {
        await Assert.ThrowsAsync<TransactionsBusinessRuleException>(
            () => Record(new DateOnly(2026, 11, 6), "Mixed", (Sales, 100m), (Supplies, 5m)));

        Assert.Empty(_tables.Movements);
    }

    [Fact]
    public async Task ListAsync_newestFirst_withoutDeleted_andPaged() // HU-05, TC-005
    {
        var first = await Record(new DateOnly(2026, 10, 1), "A", (Supplies, 1m));
        await Record(new DateOnly(2026, 10, 20), "C", (Supplies, 1m));
        await Record(new DateOnly(2026, 10, 15), "B", (Supplies, 1m));
        await _movements.DeleteAsync(Ana, first.Id, Ct);

        var page = await _movements.ListAsync(Ana, page: 1, limit: 1, Ct);
        var all = await _movements.ListAsync(Ana, page: null, limit: null, Ct);

        Assert.Equal(["C"], page.Data.Select(t => t.Description));
        Assert.Equal(new PageMeta(1, 1, 2), page.Meta);
        Assert.Equal(["C", "B"], all.Data.Select(t => t.Description));
    }

    [Fact]
    public async Task ListAsync_badPaging_isAValidationError()
    {
        await Assert.ThrowsAsync<TransactionsValidationException>(() => _movements.ListAsync(Ana, 0, 20, Ct));
    }

    [Fact]
    public async Task TotalsAsync_addsIncomeAndExpenses_withoutDeletedOnes() // BR-03, BR-04
    {
        await Record(new DateOnly(2026, 11, 1), "Sale", (Sales, 1000m));
        await Record(new DateOnly(2026, 11, 2), "Paper", (Supplies, 300m), (Supplies, 50m));
        var deleted = await Record(new DateOnly(2026, 11, 3), "Mistake", (Supplies, 100m));
        await _movements.DeleteAsync(Ana, deleted.Id, Ct);

        Assert.Equal(new Totals(1000m, 350m), await _movements.TotalsAsync(Ana, Ct));
        Assert.Equal(new Totals(0m, 0m), await _movements.TotalsAsync(Leo, Ct));
    }

    [Fact]
    public async Task DeleteAsync_twice_isNotFoundTheSecondTime() // HU-06
    {
        var transaction = await Record(new DateOnly(2026, 11, 6), "Printer paper", (Supplies, 85.50m));

        await _movements.DeleteAsync(Ana, transaction.Id, Ct);
        var error = await Assert.ThrowsAsync<TransactionsNotFoundException>(() => _movements.DeleteAsync(Ana, transaction.Id, Ct));

        Assert.Equal(ManageMovements.NotFound, error.Message);
        Assert.Single(_tables.Movements);
    }

    [Fact]
    public async Task DeleteAsync_anotherOwnersTransaction_isNotFound()
    {
        var transaction = await Record(new DateOnly(2026, 11, 6), "Printer paper", (Supplies, 85.50m));

        await Assert.ThrowsAsync<TransactionsNotFoundException>(() => _movements.DeleteAsync(Leo, transaction.Id, Ct));
        Assert.Empty(_tables.Deleted);
    }
}
