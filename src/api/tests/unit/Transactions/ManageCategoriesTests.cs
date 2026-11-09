using BudTack.Transactions.Api.Contracts;
using BudTack.Transactions.Internal.Application;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BudTack.Unit.Tests.Transactions;

/// <summary>HU-02 and HU-03 use cases against the tables in memory.</summary>
public sealed class ManageCategoriesTests
{
    private static readonly Guid Ana = Guid.Parse("00000000-0000-7000-8000-00000000000a");
    private static readonly Guid Leo = Guid.Parse("00000000-0000-7000-8000-00000000000b");

    private readonly InMemoryCategories _tables = new();
    private readonly ManageCategories _categories;

    public ManageCategoriesTests() => _categories = new ManageCategories(_tables, NullLogger<ManageCategories>.Instance);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private Task<Category> Create(Guid owner, string name, string type = "expense") =>
        _categories.CreateCategoryAsync(owner, new NewCategory(name, type), Ct);

    [Fact]
    public async Task CreateCategoryAsync_newName_isStoredActiveForItsOwner()
    {
        var category = await Create(Ana, " Oficina ");

        Assert.Equal(new Category(category.Id, "Oficina", "expense", IsActive: true), category);
        Assert.Equal(Ana, Assert.Single(_tables.Categories).OwnedBy);
    }

    [Fact]
    public async Task CreateCategoryAsync_invalid_storesNothing()
    {
        await Assert.ThrowsAsync<TransactionsValidationException>(
            () => _categories.CreateCategoryAsync(Ana, new NewCategory("", "expense"), Ct));
        Assert.Empty(_tables.Categories);
    }

    [Fact]
    public async Task ListCategoriesAsync_onlyTheOwners_byTypeThenName_withTheTotal()
    {
        await Create(Ana, "Viajes");
        await Create(Ana, "oficina");
        await Create(Ana, "Ventas", "income");
        await Create(Leo, "Ajena");

        var page = await _categories.ListCategoriesAsync(Ana, page: null, limit: null, type: null, Ct);

        Assert.Equal(["oficina", "Viajes", "Ventas"], page.Data.Select(c => c.Name));
        Assert.Equal(new PageMeta(1, 20, 3), page.Meta);
    }

    [Fact]
    public async Task ListCategoriesAsync_byType_andByPage()
    {
        await Create(Ana, "A");
        await Create(Ana, "B");
        await Create(Ana, "C");
        await Create(Ana, "Ventas", "income");

        var page = await _categories.ListCategoriesAsync(Ana, page: 2, limit: 2, type: "expense", Ct);

        Assert.Equal(["C"], page.Data.Select(c => c.Name));
        Assert.Equal(new PageMeta(2, 2, 3), page.Meta);
    }

    [Fact]
    public async Task ListCategoriesAsync_badFilter_isAValidationError()
    {
        await Assert.ThrowsAsync<TransactionsValidationException>(
            () => _categories.ListCategoriesAsync(Ana, null, null, "all", Ct));
    }

    [Fact]
    public async Task DeleteCategoryAsync_unused_removesIt()
    {
        var category = await Create(Ana, "Oficina");

        await _categories.DeleteCategoryAsync(Ana, category.Id, Ct);

        Assert.Empty(_tables.Categories);
    }

    [Fact]
    public async Task DeleteCategoryAsync_withASubcategory_isAConflict_andKeepsIt() // TC-003
    {
        var category = await Create(Ana, "Oficina");
        await _categories.CreateSubcategoryAsync(Ana, category.Id, new NewSubcategory("Papelería"), Ct);

        await Assert.ThrowsAsync<TransactionsConflictException>(() => _categories.DeleteCategoryAsync(Ana, category.Id, Ct));
        Assert.True(Assert.Single(_tables.Categories).IsActive);
    }

    [Fact]
    public async Task DeleteCategoryAsync_ofAnotherOwner_isNotFound_andKeepsIt()
    {
        var category = await Create(Leo, "Ajena");

        var error = await Assert.ThrowsAsync<TransactionsNotFoundException>(
            () => _categories.DeleteCategoryAsync(Ana, category.Id, Ct));
        Assert.Equal(ManageCategories.CategoryNotFound, error.Message);
        Assert.Single(_tables.Categories);
    }

    [Fact]
    public async Task UpdateCategoryAsync_deactivates_andReactivates()
    {
        var category = await Create(Ana, "Oficina");

        Assert.False((await _categories.UpdateCategoryAsync(Ana, category.Id, new CategoryChanges(null, false), Ct)).IsActive);
        Assert.True((await _categories.UpdateCategoryAsync(Ana, category.Id, new CategoryChanges(null, true), Ct)).IsActive);
    }

    [Fact]
    public async Task UpdateCategoryAsync_ofAnotherOwner_isNotFound()
    {
        var category = await Create(Leo, "Ajena");

        await Assert.ThrowsAsync<TransactionsNotFoundException>(
            () => _categories.UpdateCategoryAsync(Ana, category.Id, new CategoryChanges(null, false), Ct));
        Assert.True(Assert.Single(_tables.Categories).IsActive);
    }

    [Fact]
    public async Task UpdateCategoryAsync_withoutIsActive_isAValidationError()
    {
        var category = await Create(Ana, "Oficina");

        await Assert.ThrowsAsync<TransactionsValidationException>(
            () => _categories.UpdateCategoryAsync(Ana, category.Id, new CategoryChanges(null, null), Ct));
    }

    [Fact]
    public async Task CreateSubcategoryAsync_inOwnCategory_isStoredActive()
    {
        var category = await Create(Ana, "Oficina");

        var subcategory = await _categories.CreateSubcategoryAsync(Ana, category.Id, new NewSubcategory(" Papelería "), Ct);

        Assert.Equal(new Subcategory(subcategory.Id, category.Id, "Papelería", IsActive: true), subcategory);
    }

    [Fact]
    public async Task CreateSubcategoryAsync_inAnotherOwnersCategory_isNotFound_andStoresNothing()
    {
        var category = await Create(Leo, "Ajena");

        await Assert.ThrowsAsync<TransactionsNotFoundException>(
            () => _categories.CreateSubcategoryAsync(Ana, category.Id, new NewSubcategory("x"), Ct));
        Assert.Empty(_tables.Subcategories);
    }

    [Fact]
    public async Task CreateSubcategoryAsync_duplicateName_isAConflict() // TC-002
    {
        var category = await Create(Ana, "Oficina");
        await _categories.CreateSubcategoryAsync(Ana, category.Id, new NewSubcategory("Papelería"), Ct);

        await Assert.ThrowsAsync<TransactionsConflictException>(
            () => _categories.CreateSubcategoryAsync(Ana, category.Id, new NewSubcategory("PAPELERÍA"), Ct));
        Assert.Single(_tables.Subcategories);
    }

    [Fact]
    public async Task ListSubcategoriesAsync_ofOwnCategory_isPaged_byName()
    {
        var category = await Create(Ana, "Oficina");
        foreach (var name in new[] { "Tinta", "papel", "Lápices" })
        {
            await _categories.CreateSubcategoryAsync(Ana, category.Id, new NewSubcategory(name), Ct);
        }

        var page = await _categories.ListSubcategoriesAsync(Ana, category.Id, page: 1, limit: 2, Ct);

        Assert.Equal(["Lápices", "papel"], page.Data.Select(s => s.Name));
        Assert.Equal(new PageMeta(1, 2, 3), page.Meta);
    }

    [Fact]
    public async Task ListSubcategoriesAsync_ofAnotherOwnersCategory_isNotFound()
    {
        var category = await Create(Leo, "Ajena");

        await Assert.ThrowsAsync<TransactionsNotFoundException>(
            () => _categories.ListSubcategoriesAsync(Ana, category.Id, null, null, Ct));
    }

    [Fact]
    public async Task DeleteSubcategoryAsync_unused_removesIt_andLetsTheCategoryGo()
    {
        var category = await Create(Ana, "Oficina");
        var subcategory = await _categories.CreateSubcategoryAsync(Ana, category.Id, new NewSubcategory("Papelería"), Ct);

        await _categories.DeleteSubcategoryAsync(Ana, category.Id, subcategory.Id, Ct);
        await _categories.DeleteCategoryAsync(Ana, category.Id, Ct);

        Assert.Empty(_tables.Subcategories);
        Assert.Empty(_tables.Categories);
    }

    [Fact]
    public async Task DeleteSubcategoryAsync_inUse_isAConflict()
    {
        var category = await Create(Ana, "Oficina");
        var subcategory = await _categories.CreateSubcategoryAsync(Ana, category.Id, new NewSubcategory("Papelería"), Ct);
        _tables.UsedSubcategories.Add(subcategory.Id);

        await Assert.ThrowsAsync<TransactionsConflictException>(
            () => _categories.DeleteSubcategoryAsync(Ana, category.Id, subcategory.Id, Ct));
        Assert.Single(_tables.Subcategories);
    }

    [Fact]
    public async Task DeleteSubcategoryAsync_ofAnotherOwner_isNotFound()
    {
        var category = await Create(Leo, "Ajena");
        var subcategory = await _categories.CreateSubcategoryAsync(Leo, category.Id, new NewSubcategory("x"), Ct);

        var error = await Assert.ThrowsAsync<TransactionsNotFoundException>(
            () => _categories.DeleteSubcategoryAsync(Ana, category.Id, subcategory.Id, Ct));
        Assert.Equal(ManageCategories.SubcategoryNotFound, error.Message);
    }

    [Fact]
    public async Task SetSubcategoryActiveAsync_deactivates_andRefusesAnotherOwner()
    {
        var category = await Create(Ana, "Oficina");
        var subcategory = await _categories.CreateSubcategoryAsync(Ana, category.Id, new NewSubcategory("Papelería"), Ct);

        var updated = await _categories.SetSubcategoryActiveAsync(Ana, category.Id, subcategory.Id, new ActivationUpdate(false), Ct);

        Assert.False(updated.IsActive);
        await Assert.ThrowsAsync<TransactionsNotFoundException>(
            () => _categories.SetSubcategoryActiveAsync(Leo, category.Id, subcategory.Id, new ActivationUpdate(true), Ct));
    }
}
