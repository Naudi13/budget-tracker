using BudTack.Identity;
using BudTack.ModuleIntegration.Tests.Support;
using BudTack.Transactions;
using BudTack.Transactions.Api;
using BudTack.Transactions.Api.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace BudTack.ModuleIntegration.Tests.Transactions;

/// <summary>
/// HU-04 to HU-06 through <see cref="TransactionsFacade"/> only, against a real
/// PostgreSQL: lines typed by their category (BR-01), one type per transaction
/// (BR-02), soft delete (BR-04), and subcategories in use (BR-05).
/// </summary>
[Collection(PostgresTestGroup.Name)]
public sealed class MovementsFacadeTests(PostgresFixture database) : IAsyncLifetime, IDisposable
{
    private static readonly Guid Ana = Guid.Parse("00000000-0000-7000-8000-00000000000a");
    private static readonly Guid Leo = Guid.Parse("00000000-0000-7000-8000-00000000000b");

    private ServiceProvider _services = null!;
    private Subcategory _supplies = null!;
    private Subcategory _rent = null!;
    private Subcategory _store = null!;
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
        _supplies = await Facade().CreateSubcategoryAsync(Ana, _office.Id, new NewSubcategory("Papelería"), Ct);
        _rent = await Facade().CreateSubcategoryAsync(Ana, _office.Id, new NewSubcategory("Arriendo"), Ct);
        var sales = await Facade().CreateCategoryAsync(Ana, new NewCategory("Ventas", "income"), Ct);
        _store = await Facade().CreateSubcategoryAsync(Ana, sales.Id, new NewSubcategory("Tienda"), Ct);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public void Dispose() => _services.Dispose();

    private TransactionsFacade Facade() => _services.CreateScope().ServiceProvider.GetRequiredService<TransactionsFacade>();

    private Task<Transaction> Record(Guid owner, DateOnly date, string description, params (Guid Subcategory, decimal Amount)[] lines) =>
        Facade().RecordTransactionAsync(
            owner,
            new NewTransaction(date, description, [.. lines.Select(l => new NewTransactionDetail(l.Subcategory, l.Amount))]),
            Ct);

    [Fact]
    public async Task RecordTransactionAsync_storesTheHeaderAndEveryLine_toTheCent()
    {
        var transaction = await Record(Ana, new DateOnly(2026, 11, 6), "Printer paper", (_supplies.Id, 85.50m), (_rent.Id, 0.01m));

        Assert.All(transaction.Details, d => Assert.Equal("expense", d.Type));
        Assert.Equal(1L, await ScalarAsync<long>("SELECT count(*) FROM transactions.transactions"));
        Assert.Equal(85.51m, await ScalarAsync<decimal>("SELECT sum(amount) FROM transactions.transaction_details"));
    }

    [Fact]
    public async Task ListTransactionsAsync_returnsTheLinesInTheOrderTheyWereEntered()
    {
        decimal[] amounts = [9m, 2m, 7m, 1m, 8m, 3m, 6m, 4m, 5m, 10m];
        await Record(Ana, new DateOnly(2026, 11, 6), "Many lines", [.. amounts.Select(a => (_supplies.Id, a))]);

        var page = await Facade().ListTransactionsAsync(new Viewer(Ana, false), cancellationToken: Ct);

        Assert.Equal(amounts, page.Data.Single().Details.Select(d => d.Amount));
    }

    [Fact]
    public async Task RecordTransactionAsync_mixingIncomeAndExpense_storesNothing() // TC-004
    {
        var error = await Assert.ThrowsAsync<TransactionsBusinessRuleException>(
            () => Record(Ana, new DateOnly(2026, 11, 6), "Mixed", (_store.Id, 100m), (_supplies.Id, 5m)));

        Assert.Equal("details[1].subcategoryId", Assert.Single(error.Errors).Field);
        Assert.Equal(0L, await ScalarAsync<long>("SELECT count(*) FROM transactions.transactions"));
        Assert.Equal(0L, await ScalarAsync<long>("SELECT count(*) FROM transactions.transaction_details"));
    }

    [Fact]
    public async Task RecordTransactionAsync_withAnInactiveCategory_isRefused() // BR-05
    {
        await Facade().UpdateCategoryAsync(Ana, _office.Id, new CategoryChanges(null, false), Ct);

        await Assert.ThrowsAsync<TransactionsBusinessRuleException>(
            () => Record(Ana, new DateOnly(2026, 11, 6), "Paper", (_supplies.Id, 5m)));
    }

    [Fact]
    public async Task ListTransactionsAsync_newestFirst_withoutDeletedOnes_onlyTheOwners() // TC-005
    {
        var oldest = await Record(Ana, new DateOnly(2026, 10, 1), "A", (_supplies.Id, 1m));
        await Record(Ana, new DateOnly(2026, 10, 20), "C", (_supplies.Id, 2m), (_rent.Id, 3m));
        await Record(Ana, new DateOnly(2026, 10, 15), "B", (_store.Id, 4m));
        await Facade().DeleteTransactionAsync(new Viewer(Ana, false), oldest.Id, new TransactionDeletion("Monto equivocado"), Ct);

        var page = await Facade().ListTransactionsAsync(new Viewer(Ana, false), cancellationToken: Ct);
        var others = await Facade().ListTransactionsAsync(new Viewer(Leo, false), cancellationToken: Ct);

        Assert.Equal(["C", "B"], page.Data.Select(t => t.Description));
        Assert.Equal(2L, page.Meta.Total);
        Assert.Equal([2m, 3m], page.Data[0].Details.Select(d => d.Amount));
        Assert.Equal("income", Assert.Single(page.Data[1].Details).Type);
        Assert.Equal(0L, others.Meta.Total);
    }

    [Fact]
    public async Task DeleteTransactionAsync_isSoft_keepingTheRowAndWhoDeletedIt() // TC-006
    {
        var transaction = await Record(Ana, new DateOnly(2026, 11, 6), "Printer paper", (_supplies.Id, 85.50m));

        await Facade().DeleteTransactionAsync(new Viewer(Ana, false), transaction.Id, new TransactionDeletion("Monto equivocado"), Ct);

        await using var command = database.DataSource.CreateCommand(
            "SELECT deleted_by, deleted_at > now() - interval '1 minute', deletion_reason FROM transactions.transactions WHERE id = $1");
        command.Parameters.Add(new NpgsqlParameter { Value = transaction.Id });
        await using var row = await command.ExecuteReaderAsync(Ct);
        Assert.True(await row.ReadAsync(Ct));
        Assert.Equal(Ana, row.GetGuid(0));
        Assert.True(row.GetBoolean(1));
        Assert.Equal("Monto equivocado", row.GetString(2));
    }

    [Fact]
    public async Task DeleteTransactionAsync_twice_orByAnotherOwner_isNotFound()
    {
        var transaction = await Record(Ana, new DateOnly(2026, 11, 6), "Printer paper", (_supplies.Id, 85.50m));

        await Assert.ThrowsAsync<TransactionsNotFoundException>(() => Facade().DeleteTransactionAsync(new Viewer(Leo, false), transaction.Id, new TransactionDeletion("Monto equivocado"), Ct));
        await Facade().DeleteTransactionAsync(new Viewer(Ana, false), transaction.Id, new TransactionDeletion("Monto equivocado"), Ct);
        await Assert.ThrowsAsync<TransactionsNotFoundException>(() => Facade().DeleteTransactionAsync(new Viewer(Ana, false), transaction.Id, new TransactionDeletion("Monto equivocado"), Ct));
    }

    [Fact]
    public async Task DeleteSubcategoryAsync_usedByATransaction_isAConflict_evenAfterTheTransactionIsDeleted() // BR-05
    {
        var transaction = await Record(Ana, new DateOnly(2026, 11, 6), "Printer paper", (_supplies.Id, 85.50m));
        await Facade().DeleteTransactionAsync(new Viewer(Ana, false), transaction.Id, new TransactionDeletion("Monto equivocado"), Ct);

        await Assert.ThrowsAsync<TransactionsConflictException>(
            () => Facade().DeleteSubcategoryAsync(_office.Id, _supplies.Id, Ct));
        await Facade().DeleteSubcategoryAsync(_office.Id, _rent.Id, Ct);
    }

    private async Task<T> ScalarAsync<T>(string sql)
    {
        await using var command = database.DataSource.CreateCommand(sql);
        return (T)(await command.ExecuteScalarAsync(Ct))!;
    }
}
