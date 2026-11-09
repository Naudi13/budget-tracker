using BudTack.Identity;
using BudTack.ModuleIntegration.Tests.Support;
using BudTack.Transactions;
using BudTack.Transactions.Api;
using BudTack.Transactions.Api.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BudTack.ModuleIntegration.Tests.Transactions;

/// <summary>
/// HU-02 and HU-03 through <see cref="TransactionsFacade"/> only, against
/// transactions.categories and .subcategories in a real PostgreSQL: owners kept
/// apart, unique names ignoring case, and ON DELETE RESTRICT (BR-05).
/// </summary>
[Collection(PostgresTestGroup.Name)]
public sealed class CategoriesFacadeTests(PostgresFixture database) : IAsyncLifetime, IDisposable
{
    private static readonly Guid Ana = Guid.Parse("00000000-0000-7000-8000-00000000000a");
    private static readonly Guid Leo = Guid.Parse("00000000-0000-7000-8000-00000000000b");

    private ServiceProvider _services = null!;

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
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public void Dispose() => _services.Dispose();

    private TransactionsFacade Facade() => _services.CreateScope().ServiceProvider.GetRequiredService<TransactionsFacade>();

    private Task<Category> Create(Guid owner, string name, string type = "expense") =>
        Facade().CreateCategoryAsync(owner, new NewCategory(name, type), Ct);

    [Fact]
    public async Task CreateCategoryAsync_isStoredForItsOwner_withAuditColumns()
    {
        var category = await Create(Ana, "Oficina");

        await using var command = database.DataSource.CreateCommand(
            "SELECT owned_by, created_by, updated_by, is_active FROM transactions.categories WHERE id = $1");
        command.Parameters.Add(new Npgsql.NpgsqlParameter { Value = category.Id });
        await using var row = await command.ExecuteReaderAsync(Ct);
        Assert.True(await row.ReadAsync(Ct));
        Assert.Equal(Ana, row.GetGuid(0));
        Assert.Equal(Ana, row.GetGuid(1));
        Assert.Equal(Ana, row.GetGuid(2));
        Assert.True(row.GetBoolean(3));
    }

    [Fact]
    public async Task CreateCategoryAsync_sameNameAndType_inAnyCase_isAConflict_butNotForAnotherTypeOrOwner()
    {
        await Create(Ana, "Oficina");

        await Assert.ThrowsAsync<TransactionsConflictException>(() => Create(Ana, "OFICINA"));
        await Create(Ana, "Oficina", "income");
        await Create(Leo, "Oficina");
        Assert.Equal(3L, await CountAsync("transactions.categories"));
    }

    [Fact]
    public async Task ListCategoriesAsync_showsOnlyTheOwnersRows_inOrder_andPaged()
    {
        await Create(Ana, "Viajes");
        await Create(Ana, "oficina");
        await Create(Ana, "Ventas", "income");
        await Create(Leo, "Ajena");

        var all = await Facade().ListCategoriesAsync(Ana, cancellationToken: Ct);
        var expenses = await Facade().ListCategoriesAsync(Ana, page: 2, limit: 1, type: "expense", cancellationToken: Ct);

        Assert.Equal(["oficina", "Viajes", "Ventas"], all.Data.Select(c => c.Name));
        Assert.Equal(new PageMeta(1, 20, 3), all.Meta);
        Assert.Equal(["Viajes"], expenses.Data.Select(c => c.Name));
        Assert.Equal(new PageMeta(2, 1, 2), expenses.Meta);
    }

    [Fact]
    public async Task DeleteCategoryAsync_withASubcategory_isRefusedByTheDatabase_andKeepsTheRow() // TC-003
    {
        var category = await Create(Ana, "Oficina");
        await Facade().CreateSubcategoryAsync(Ana, category.Id, new NewSubcategory("Papelería"), Ct);

        await Assert.ThrowsAsync<TransactionsConflictException>(() => Facade().DeleteCategoryAsync(Ana, category.Id, Ct));

        Assert.Equal(1L, await CountAsync("transactions.categories"));
        Assert.True((await Facade().ListCategoriesAsync(Ana, cancellationToken: Ct)).Data.Single().IsActive);
    }

    [Fact]
    public async Task DeleteCategoryAsync_ofAnotherOwner_isNotFound_andKeepsTheRow()
    {
        var category = await Create(Leo, "Ajena");

        await Assert.ThrowsAsync<TransactionsNotFoundException>(() => Facade().DeleteCategoryAsync(Ana, category.Id, Ct));
        Assert.Equal(1L, await CountAsync("transactions.categories"));
    }

    [Fact]
    public async Task UpdateCategoryAsync_onlyForItsOwner()
    {
        var category = await Create(Ana, "Oficina");

        var off = await Facade().UpdateCategoryAsync(Ana, category.Id, new CategoryChanges(null, false), Ct);

        Assert.False(off.IsActive);
        await Assert.ThrowsAsync<TransactionsNotFoundException>(
            () => Facade().UpdateCategoryAsync(Leo, category.Id, new CategoryChanges(null, true), Ct));
    }

    [Fact]
    public async Task Subcategories_create_listDeactivateAndDelete_withinTheOwnersCategory() // TC-002
    {
        var category = await Create(Ana, "Oficina");
        var paper = await Facade().CreateSubcategoryAsync(Ana, category.Id, new NewSubcategory("Papelería"), Ct);
        await Facade().CreateSubcategoryAsync(Ana, category.Id, new NewSubcategory("Tinta"), Ct);

        await Assert.ThrowsAsync<TransactionsConflictException>(
            () => Facade().CreateSubcategoryAsync(Ana, category.Id, new NewSubcategory("papelería"), Ct));
        var off = await Facade().SetSubcategoryActiveAsync(Ana, category.Id, paper.Id, new ActivationUpdate(false), Ct);
        var page = await Facade().ListSubcategoriesAsync(Ana, category.Id, cancellationToken: Ct);

        Assert.False(off.IsActive);
        Assert.Equal(["Papelería", "Tinta"], page.Data.Select(s => s.Name));
        Assert.Equal(2L, page.Meta.Total);

        await Facade().DeleteSubcategoryAsync(Ana, category.Id, paper.Id, Ct);
        Assert.Equal(1L, await CountAsync("transactions.subcategories"));
    }

    [Fact]
    public async Task Subcategories_ofAnotherOwnersCategory_areNotFound()
    {
        var category = await Create(Leo, "Ajena");
        var subcategory = await Facade().CreateSubcategoryAsync(Leo, category.Id, new NewSubcategory("x"), Ct);

        await Assert.ThrowsAsync<TransactionsNotFoundException>(
            () => Facade().ListSubcategoriesAsync(Ana, category.Id, cancellationToken: Ct));
        await Assert.ThrowsAsync<TransactionsNotFoundException>(
            () => Facade().CreateSubcategoryAsync(Ana, category.Id, new NewSubcategory("y"), Ct));
        await Assert.ThrowsAsync<TransactionsNotFoundException>(
            () => Facade().DeleteSubcategoryAsync(Ana, category.Id, subcategory.Id, Ct));
        await Assert.ThrowsAsync<TransactionsNotFoundException>(
            () => Facade().SetSubcategoryActiveAsync(Ana, category.Id, subcategory.Id, new ActivationUpdate(false), Ct));
        Assert.Equal(1L, await CountAsync("transactions.subcategories"));
    }

    [Fact]
    public async Task CreateCategoryAsync_twoAtOnceWithOneName_storesOne()
    {
        var attempts = await Task.WhenAll(Enumerable.Range(0, 2).Select(async _ =>
        {
            try
            {
                await Create(Ana, "Carrera");
                return "created";
            }
            catch (TransactionsConflictException)
            {
                return "conflict";
            }
        }));

        Assert.Equal(["conflict", "created"], attempts.Order());
    }

    private async Task<long> CountAsync(string table)
    {
        await using var command = database.DataSource.CreateCommand($"SELECT count(*) FROM {table}");
        return (long)(await command.ExecuteScalarAsync(Ct))!;
    }
}
