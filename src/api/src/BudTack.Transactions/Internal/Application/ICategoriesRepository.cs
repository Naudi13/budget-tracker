using BudTack.Transactions.Internal.Domain;

namespace BudTack.Transactions.Internal.Application;

/// <summary>
/// transactions.categories and .subcategories, always seen through one owner:
/// a row of another owner behaves exactly like a row that does not exist.
/// </summary>
internal interface ICategoriesRepository
{
    Task<(IReadOnlyList<Category> Rows, long Total)> ListCategoriesAsync(
        Guid owner, string? type, Paging paging, CancellationToken cancellationToken);

    Task<Category?> FindCategoryAsync(Guid owner, Guid categoryId, CancellationToken cancellationToken);

    /// <summary>Stores a category; throws <c>TransactionsConflictException</c> when the name is taken.</summary>
    Task AddCategoryAsync(Category category, CancellationToken cancellationToken);

    /// <summary>False when there was no such category; throws <c>TransactionsConflictException</c> when it is in use (BR-05).</summary>
    Task<bool> DeleteCategoryAsync(Guid owner, Guid categoryId, CancellationToken cancellationToken);

    Task<Category?> SetCategoryActiveAsync(Guid owner, Guid categoryId, bool isActive, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Subcategory> Rows, long Total)> ListSubcategoriesAsync(
        Guid categoryId, Paging paging, CancellationToken cancellationToken);

    /// <summary>Stores a subcategory; throws <c>TransactionsConflictException</c> when the name is taken.</summary>
    Task AddSubcategoryAsync(Subcategory subcategory, Guid owner, CancellationToken cancellationToken);

    /// <summary>False when there was no such subcategory; throws <c>TransactionsConflictException</c> when it is in use (BR-05).</summary>
    Task<bool> DeleteSubcategoryAsync(Guid owner, Guid categoryId, Guid subcategoryId, CancellationToken cancellationToken);

    Task<Subcategory?> SetSubcategoryActiveAsync(
        Guid owner, Guid categoryId, Guid subcategoryId, bool isActive, CancellationToken cancellationToken);
}
