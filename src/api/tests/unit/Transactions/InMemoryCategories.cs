using BudTack.Transactions.Api.Contracts;
using BudTack.Transactions.Internal.Application;
using BudTack.Transactions.Internal.Domain;
using DomainCategory = BudTack.Transactions.Internal.Domain.Category;
using DomainSubcategory = BudTack.Transactions.Internal.Domain.Subcategory;

namespace BudTack.Unit.Tests.Transactions;

/// <summary>
/// transactions.categories and .subcategories in memory, with the database's
/// behaviour: rows seen through their owner, unique names ignoring case, and
/// ON DELETE RESTRICT from subcategories.
/// </summary>
internal sealed class InMemoryCategories : ICategoriesRepository
{
    public List<DomainCategory> Categories { get; } = [];

    public List<DomainSubcategory> Subcategories { get; } = [];

    /// <summary>Subcategory ids a transaction detail points at (HU-04 stands in here).</summary>
    public HashSet<Guid> UsedSubcategories { get; } = [];

    public Task<(IReadOnlyList<DomainCategory> Rows, long Total)> ListCategoriesAsync(
        Guid owner, string? type, Paging paging, CancellationToken cancellationToken)
    {
        var all = Categories
            .Where(c => c.OwnedBy == owner && (type is null || c.Type == type))
            .OrderBy(c => c.Type, StringComparer.Ordinal)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return Task.FromResult<(IReadOnlyList<DomainCategory>, long)>(
            ([.. all.Skip(paging.Offset).Take(paging.Limit)], all.Count));
    }

    public Task<DomainCategory?> FindCategoryAsync(Guid owner, Guid categoryId, CancellationToken cancellationToken) =>
        Task.FromResult(Categories.Find(c => c.Id == categoryId && c.OwnedBy == owner));

    public Task AddCategoryAsync(DomainCategory category, CancellationToken cancellationToken)
    {
        if (Categories.Exists(c => c.OwnedBy == category.OwnedBy && c.Type == category.Type
            && string.Equals(c.Name, category.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new TransactionsConflictException("name taken");
        }

        Categories.Add(category);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteCategoryAsync(Guid owner, Guid categoryId, CancellationToken cancellationToken)
    {
        var category = Categories.Find(c => c.Id == categoryId && c.OwnedBy == owner);
        if (category is null)
        {
            return Task.FromResult(false);
        }

        if (Subcategories.Exists(s => s.CategoryId == categoryId))
        {
            throw new TransactionsConflictException("in use");
        }

        Categories.Remove(category);
        return Task.FromResult(true);
    }

    public Task<DomainCategory?> UpdateCategoryAsync(
        Guid owner, Guid categoryId, string? name, bool? isActive, CancellationToken cancellationToken)
    {
        var index = Categories.FindIndex(c => c.Id == categoryId && c.OwnedBy == owner);
        if (index < 0)
        {
            return Task.FromResult<DomainCategory?>(null);
        }

        var current = Categories[index];
        if (name is not null && Categories.Exists(c => c.Id != categoryId && c.OwnedBy == owner && c.Type == current.Type
            && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new TransactionsConflictException("name taken");
        }

        Categories[index] = current with { Name = name ?? current.Name, IsActive = isActive ?? current.IsActive };
        return Task.FromResult<DomainCategory?>(Categories[index]);
    }

    public Task<(IReadOnlyList<DomainSubcategory> Rows, long Total)> ListSubcategoriesAsync(
        Guid categoryId, Paging paging, CancellationToken cancellationToken)
    {
        var all = Subcategories.Where(s => s.CategoryId == categoryId)
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList();
        return Task.FromResult<(IReadOnlyList<DomainSubcategory>, long)>(
            ([.. all.Skip(paging.Offset).Take(paging.Limit)], all.Count));
    }

    public Task AddSubcategoryAsync(DomainSubcategory subcategory, Guid owner, CancellationToken cancellationToken)
    {
        if (Subcategories.Exists(s => s.CategoryId == subcategory.CategoryId
            && string.Equals(s.Name, subcategory.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new TransactionsConflictException("name taken");
        }

        Subcategories.Add(subcategory);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteSubcategoryAsync(
        Guid owner, Guid categoryId, Guid subcategoryId, CancellationToken cancellationToken)
    {
        var subcategory = Owned(owner, categoryId, subcategoryId);
        if (subcategory is null)
        {
            return Task.FromResult(false);
        }

        if (UsedSubcategories.Contains(subcategoryId))
        {
            throw new TransactionsConflictException("in use");
        }

        Subcategories.Remove(subcategory);
        return Task.FromResult(true);
    }

    public Task<DomainSubcategory?> SetSubcategoryActiveAsync(
        Guid owner, Guid categoryId, Guid subcategoryId, bool isActive, CancellationToken cancellationToken)
    {
        var subcategory = Owned(owner, categoryId, subcategoryId);
        if (subcategory is null)
        {
            return Task.FromResult<DomainSubcategory?>(null);
        }

        var updated = subcategory with { IsActive = isActive };
        Subcategories[Subcategories.IndexOf(subcategory)] = updated;
        return Task.FromResult<DomainSubcategory?>(updated);
    }

    private DomainSubcategory? Owned(Guid owner, Guid categoryId, Guid subcategoryId) =>
        Categories.Exists(c => c.Id == categoryId && c.OwnedBy == owner)
            ? Subcategories.Find(s => s.Id == subcategoryId && s.CategoryId == categoryId)
            : null;
}
