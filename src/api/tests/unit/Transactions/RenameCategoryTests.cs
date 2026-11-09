using BudTack.Transactions.Api.Contracts;
using BudTack.Transactions.Internal.Application;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BudTack.Unit.Tests.Transactions;

/// <summary>HU-03: a category's PATCH changes only what it brings (TC-021).</summary>
public sealed class RenameCategoryTests
{
    private static readonly Guid Ana = Guid.Parse("0193a1b2-0000-7000-8000-00000000000a");

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly ManageCategories _categories = new(new InMemoryCategories(), NullLogger<ManageCategories>.Instance);

    private Task<Category> Create(string name) => _categories.CreateCategoryAsync(Ana, new NewCategory(name, "expense"), Ct);

    [Fact]
    public async Task UpdateCategoryAsync_aNewName_keepsTypeAndState()
    {
        var category = await Create("Oficina");

        var renamed = await _categories.UpdateCategoryAsync(Ana, category.Id, new CategoryChanges(" Papelería ", null), Ct);

        Assert.Equal(category with { Name = "Papelería" }, renamed);
    }

    [Fact]
    public async Task UpdateCategoryAsync_onlyIsActive_keepsTheName()
    {
        var category = await Create("Oficina");

        var off = await _categories.UpdateCategoryAsync(Ana, category.Id, new CategoryChanges(null, false), Ct);

        Assert.Equal(category with { IsActive = false }, off);
    }

    [Fact]
    public async Task UpdateCategoryAsync_aNameTakenInTheType_isAConflict()
    {
        var category = await Create("Oficina");
        await Create("Arriendo");

        await Assert.ThrowsAsync<TransactionsConflictException>(
            () => _categories.UpdateCategoryAsync(Ana, category.Id, new CategoryChanges("arriendo", null), Ct));
    }

    [Fact]
    public async Task UpdateCategoryAsync_itsOwnNameInOtherCase_isAllowed()
    {
        var category = await Create("Oficina");

        var renamed = await _categories.UpdateCategoryAsync(Ana, category.Id, new CategoryChanges("OFICINA", null), Ct);

        Assert.Equal("OFICINA", renamed.Name);
    }

    [Theory]
    [InlineData("   ", "name")]
    [InlineData(null, "body")]
    public async Task UpdateCategoryAsync_nothingUsable_namesTheField(string? name, string field)
    {
        var category = await Create("Oficina");

        var error = await Assert.ThrowsAsync<TransactionsValidationException>(
            () => _categories.UpdateCategoryAsync(Ana, category.Id, new CategoryChanges(name, null), Ct));

        Assert.Equal(field, Assert.Single(error.Errors).Field);
    }

}
