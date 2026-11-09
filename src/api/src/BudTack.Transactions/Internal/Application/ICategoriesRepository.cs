using BudTack.Transactions.Internal.Domain;

namespace BudTack.Transactions.Internal.Application;

/// <summary>
/// transactions.categories and .subcategories, shared by the whole business
/// (ADR-009). <c>actor</c> is who makes a change, for the audit columns.
/// </summary>
internal interface ICategoriesRepository
{
    Task<(IReadOnlyList<Category> Rows, long Total)> ListCategoriesAsync(
        string? type, Paging paging, CancellationToken cancellationToken);

    Task<Category?> FindCategoryAsync(Guid categoryId, CancellationToken cancellationToken);

    /// <summary>Stores a category; throws <c>TransactionsConflictException</c> when the name is taken.</summary>
    Task AddCategoryAsync(Category category, Guid actor, CancellationToken cancellationToken);

    /// <summary>False when there was no such category; throws <c>TransactionsConflictException</c> when it is in use (BR-05).</summary>
    Task<bool> DeleteCategoryAsync(Guid categoryId, CancellationToken cancellationToken);

    /// <summary>Changes the fields that are not null; throws <c>TransactionsConflictException</c> when the name is taken.</summary>
    Task<Category?> UpdateCategoryAsync(Guid actor, Guid categoryId, string? name, bool? isActive, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Subcategory> Rows, long Total)> ListSubcategoriesAsync(
        Guid categoryId, Paging paging, CancellationToken cancellationToken);

    /// <summary>Stores a subcategory; throws <c>TransactionsConflictException</c> when the name is taken.</summary>
    Task AddSubcategoryAsync(Subcategory subcategory, Guid actor, CancellationToken cancellationToken);

    /// <summary>False when there was no such subcategory; throws <c>TransactionsConflictException</c> when it is in use (BR-05).</summary>
    Task<bool> DeleteSubcategoryAsync(Guid categoryId, Guid subcategoryId, CancellationToken cancellationToken);

    Task<Subcategory?> SetSubcategoryActiveAsync(
        Guid actor, Guid categoryId, Guid subcategoryId, bool isActive, CancellationToken cancellationToken);
}
