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
    private readonly InMemoryAccountNames _names = new();
    private readonly ManageMovements _movements;

    public ManageMovementsTests()
    {
        _tables.Subcategories[Supplies] = (Ana, new SubcategoryUse(Supplies, "expense", IsUsable: true));
        _tables.Subcategories[Sales] = (Ana, new SubcategoryUse(Sales, "income", IsUsable: true));
        _tables.Subcategories[LeosSubcategory] = (Leo, new SubcategoryUse(LeosSubcategory, "expense", IsUsable: true));
        _movements = new ManageMovements(_tables, _names, NullLogger<ManageMovements>.Instance);
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
        await _movements.DeleteAsync(new Viewer(Ana, false), first.Id, new TransactionDeletion("Monto equivocado"), Ct);

        var page = await _movements.ListAsync(new Viewer(Ana, false), page: 1, limit: 1, deleted: false, Ct);
        var all = await _movements.ListAsync(new Viewer(Ana, false), page: null, limit: null, deleted: false, Ct);

        Assert.Equal(["C"], page.Data.Select(t => t.Description));
        Assert.Equal(new PageMeta(1, 1, 2), page.Meta);
        Assert.Equal(["C", "B"], all.Data.Select(t => t.Description));
    }

    [Fact]
    public async Task ListAsync_badPaging_isAValidationError()
    {
        await Assert.ThrowsAsync<TransactionsValidationException>(() => _movements.ListAsync(new Viewer(Ana, false), 0, 20, deleted: false, Ct));
    }

    [Fact]
    public async Task TotalsAsync_addsIncomeAndExpenses_withoutDeletedOnes() // BR-03, BR-04
    {
        await Record(new DateOnly(2026, 11, 1), "Sale", (Sales, 1000m));
        await Record(new DateOnly(2026, 11, 2), "Paper", (Supplies, 300m), (Supplies, 50m));
        var deleted = await Record(new DateOnly(2026, 11, 3), "Mistake", (Supplies, 100m));
        await _movements.DeleteAsync(new Viewer(Ana, false), deleted.Id, new TransactionDeletion("Monto equivocado"), Ct);

        Assert.Equal(new Totals(1000m, 350m), await _movements.TotalsAsync(new Viewer(Ana, false), Ct));
        Assert.Equal(new Totals(0m, 0m), await _movements.TotalsAsync(new Viewer(Leo, false), Ct));
    }

    [Fact]
    public async Task DeleteAsync_twice_isNotFoundTheSecondTime() // HU-06
    {
        var transaction = await Record(new DateOnly(2026, 11, 6), "Printer paper", (Supplies, 85.50m));

        await _movements.DeleteAsync(new Viewer(Ana, false), transaction.Id, new TransactionDeletion("Monto equivocado"), Ct);
        var error = await Assert.ThrowsAsync<TransactionsNotFoundException>(() => _movements.DeleteAsync(new Viewer(Ana, false), transaction.Id, new TransactionDeletion("Monto equivocado"), Ct));

        Assert.Equal(ManageMovements.NotFound, error.Message);
        Assert.Single(_tables.Movements);
    }

    [Fact]
    public async Task DeleteAsync_anotherOwnersTransaction_isNotFound()
    {
        var transaction = await Record(new DateOnly(2026, 11, 6), "Printer paper", (Supplies, 85.50m));

        await Assert.ThrowsAsync<TransactionsNotFoundException>(() => _movements.DeleteAsync(new Viewer(Leo, false), transaction.Id, new TransactionDeletion("Monto equivocado"), Ct));
        Assert.Empty(_tables.Deleted);
    }

    private static Viewer AdminViewer => new(Guid.Parse("00000000-0000-7000-8000-0000000000ad"), true);

    [Fact]
    public async Task ListAsync_asAnAdmin_seesEveryAccount_withWhoRecordedIt() // HU-05
    {
        _names.Names[Ana] = "Ana";
        await Record(new DateOnly(2026, 11, 6), "De Ana", (Supplies, 5m));

        var page = await _movements.ListAsync(AdminViewer, null, null, deleted: false, Ct);
        var asLeo = await _movements.ListAsync(new Viewer(Leo, false), null, null, deleted: false, Ct);

        Assert.Equal("Ana", Assert.Single(page.Data).RecordedBy);
        Assert.Empty(asLeo.Data);
    }

    [Fact]
    public async Task ListAsync_deleted_isOnlyForAnAdmin_withWhoWhenAndWhy() // TC-026
    {
        _names.Names[Ana] = "Ana";
        var transaction = await Record(new DateOnly(2026, 11, 6), "De Ana", (Supplies, 5m));
        await _movements.DeleteAsync(new Viewer(Ana, false), transaction.Id, new TransactionDeletion(" Monto equivocado "), Ct);

        var deleted = await _movements.ListAsync(AdminViewer, null, null, deleted: true, Ct);

        var row = Assert.Single(deleted.Data);
        Assert.Equal("Monto equivocado", row.Deletion?.Reason);
        Assert.Equal("Ana", row.Deletion?.DeletedBy);
        await Assert.ThrowsAsync<TransactionsForbiddenException>(
            () => _movements.ListAsync(new Viewer(Ana, false), null, null, deleted: true, Ct));
    }

    [Fact]
    public async Task DeleteAsync_asAnAdmin_reachesAnyTransaction() // TC-026
    {
        var transaction = await Record(new DateOnly(2026, 11, 6), "De Ana", (Supplies, 5m));

        await _movements.DeleteAsync(AdminViewer, transaction.Id, new TransactionDeletion("Duplicado"), Ct);

        Assert.Equal(AdminViewer.UserId, _tables.Deletions[transaction.Id].DeletedBy);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  a ")]
    public async Task DeleteAsync_withoutAUsableReason_isAValidationError(string? reason) // TC-026
    {
        var transaction = await Record(new DateOnly(2026, 11, 6), "De Ana", (Supplies, 5m));

        var error = await Assert.ThrowsAsync<TransactionsValidationException>(
            () => _movements.DeleteAsync(new Viewer(Ana, false), transaction.Id, new TransactionDeletion(reason), Ct));

        Assert.Equal("reason", Assert.Single(error.Errors).Field);
        Assert.Empty(_tables.Deleted);
    }
}
